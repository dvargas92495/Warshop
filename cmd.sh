#!/bin/sh

set -e

PRODUCTION_ALIAS="Z8_App";

helpCmd() {
    echo "You could use the following commands:";
    echo "    help: prints all available commands to console";
    echo "    clean: deletes all the unused builds on aws";
	echo "    connect: connect to the current fleet instance";
	echo "    deploy: deploys the app server to aws";
	echo "    local: launch Amazon Gamelift Local to test deployment locally"
	echo "    open: opens the unity project and vs";
	echo "    server: builds the app server";
}

cleanCmd(){
	FLEET_BUILDS=$(aws gamelift describe-fleet-attributes --query "FleetAttributes[*].BuildId" --output text);
    ALL_BUILDS=$(aws gamelift list-builds --query "Builds[*].BuildId" --output text | head --bytes -2);
    for BUILD in $ALL_BUILDS
    do
    	if [[ "$FLEET_BUILDS" = *"$BUILD"* ]]; then
    		echo "Valid Build $BUILD";
        else
		    aws gamelift delete-build --build-id ${BUILD:-1};
        	echo "Deleted $BUILD";
        fi
    done
}

connectCmd(){
    rm -f MyPrivateKey.pem;
	ALIAS_ID=$(aws gamelift list-aliases --query "Aliases[?Name=='$PRODUCTION_ALIAS'].AliasId" --output text | head --bytes -2);
	FLEET_ID=$(aws gamelift describe-alias --alias-id $ALIAS_ID --query "Alias.RoutingStrategy.FleetId" --output text | head --bytes -2);
    INSTANCE_ID=$(aws gamelift describe-instances --fleet-id $FLEET_ID --query "Instances[0].InstanceId" --output text | head --bytes -2);
    IP_ADDRESS=$(aws gamelift describe-instances --fleet-id $FLEET_ID --query "Instances[0].IpAddress" --output text | head --bytes -2);
	aws gamelift get-instance-access --fleet-id $FLEET_ID --instance-id $INSTANCE_ID --query "InstanceAccess.Credentials.Secret" --output text | head --bytes -2 > MyPrivateKey.pem;
	USER=$(aws gamelift get-instance-access --fleet-id $FLEET_ID --instance-id $INSTANCE_ID --query "InstanceAccess.Credentials.UserName" --output text | head --bytes -2);
	chmod 400 MyPrivateKey.pem;
	echo "Trying to connect to $FLEET_ID as $USER@$IP_ADDRESS...";
	ssh -i MyPrivateKey.pem $USER@$IP_ADDRESS;
}

FLEET_STATUS="";
fleetStatus(){
    TIME=$(date);
    FLEET_STATUS=$(aws gamelift describe-fleet-attributes --fleet-id $1 --query "FleetAttributes[0].Status" --output text | head --bytes -2)
	echo "$TIME: New Fleet Status For $1 - $FLEET_STATUS";
	sleep 60;
}

deployCmd() {
	BUILD_LINE=$(aws gamelift upload-build --name Warshop --build-version 2020.113.0 --build-root $PWD/ServerBuild --operating-system AMAZON_LINUX | tail -1);
    BUILD_ID=$(echo ${BUILD_LINE:10:42});
	echo "Build $BUILD_ID Uploaded Successfully";
	STATUS=$(aws gamelift describe-build --build-id $BUILD_ID  --query "Build.Status" --output text);
	if [[ $STATUS = "READY"* ]]; then
	    echo "Build Ready, Creating Fleet...";
	    RESULT=$(aws gamelift create-fleet --name "Warshop" --description "Warshop App Server" --build-id $BUILD_ID --ec2-instance-type "c4.large" --runtime-configuration "GameSessionActivationTimeoutSeconds=600, ServerProcesses=[{LaunchPath=/local/game/App.x86_64, ConcurrentExecutions=10}]" --new-game-session-protection-policy "FullProtection" --ec2-inbound-permissions "FromPort=12350,ToPort=12359,IpRange=0.0.0.0/0,Protocol=UDP" --query "FleetAttributes.Status" --output text);
	    if [[ $RESULT = "NEW"* ]]; then
		    echo "Fleet Created!";
		    NEW_FLEET_ID=$(aws gamelift describe-fleet-attributes --query "FleetAttributes[?BuildId=='$BUILD_ID'].FleetId" --output text | head --bytes -2);
			aws gamelift put-scaling-policy --fleet-id $NEW_FLEET_ID --name "No Instances" --scaling-adjustment 0 --scaling-adjustment-type "ExactCapacity" --threshold 1 --comparison-operator "LessThanThreshold" --evaluation-periods 30 --metric-name "ActiveGameSessions";
			ALIAS_ID=$(aws gamelift list-aliases --query "Aliases[?Name=='$PRODUCTION_ALIAS'].AliasId" --output text | head --bytes -2);
			until echo $FLEET_STATUS | grep -m 1 "ACTIVE"; do fleetStatus $NEW_FLEET_ID; done
			OLD_FLEET_ID=$(aws gamelift describe-alias --alias-id $ALIAS_ID --query "Alias.RoutingStrategy.FleetId" --output text | head --bytes -2);
			aws gamelift update-alias --alias-id $ALIAS_ID --routing-strategy "Type=SIMPLE,FleetId=$NEW_FLEET_ID";
			aws gamelift delete-fleet --fleet-id $OLD_FLEET_ID;
			echo "Alias $ALIAS_ID updated FleetId from: $OLD_FLEET_ID to: $NEW_FLEET_ID";
		else
		    echo $RESULT;
			echo "Fleet failed to be created";
		fi
    else
		echo "Build $BUILD_ID Not Ready, STATUS=$STATUS";
	fi 
}

localCmd() {
	unity -quit -batchmode -nographics -buildWindows64Player $Z8_HOME/ServerBuild/LocalApp.exe -projectPath $Z8_HOME;
	ServerBuild/LocalApp.exe -batchmode -nographics;
    java -jar GameLiftLocal.jar -p 12345;
}

openCmd() {
    unity -projectPath $Z8_HOME;
}

serverCmd() {
	rm server.zip
	Unity -quit -batchmode -nographics -buildWindows64Player $PWD/ServerBuild/App.exe -projectPath $PWD -executeMethod BuildServer.Start;
	cd ServerBuild
	zip -r ../server.zip .
	cd ..
	rm -Rf ServerBuild 
}

lambdaCmd() {
	cd Lambda/$1
	dotnet build $1.csproj
	cd bin/Debug/netcoreapp3.1
	zip -r ../../../$1.zip .
}

noCmd(){
    echo "No command entered.";
	helpCmd;
}

invalidCmd(){
	echo "Invalid Command '$1' entered.";
	helpCmd;
}

if [[ $1 = "help" ]]; then
    helpCmd;
elif [[ $1 = "clean" ]]; then
	cleanCmd;
elif [[ $1 = "connect" ]]; then
    connectCmd;
elif [[ $1 = "deploy" ]]; then
    deployCmd;
elif [[ $1 = "local" ]]; then
    localCmd;
elif [[ $1 = "open" ]]; then
    openCmd;
elif [[ $1 = "server" ]]; then
    serverCmd;
elif [[ $1 = "lambda" ]]; then
    lambdaCmd $2;
elif [[ $1 = "" ]]; then
    noCmd;
else
    invalidCmd $1;
fi