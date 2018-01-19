#!/bin/sh

helpCmd() {
    echo "You could use the following commands:";
    echo "    help: prints all available commands to console";
    echo "    clean: deletes all the unused builds on aws";
	echo "    deploy: deploys the app server to aws";
	echo "    open: opens the unity project and vs";
	echo "    server: builds the app server";
}

cleanCmd(){
	FLEET_BUILDS=$(aws gamelift describe-fleet-attributes --query "FleetAttributes[*].BuildId" --output text);
    ALL_BUILDS=$(aws gamelift list-builds --query "Builds[*].BuildId" --output text);
    for BUILD in $ALL_BUILDS
    do
    	if [[ $BUILD = *$FLEET_BUILDS* ]]; then
    		echo "Valid Build $BUILD";
        else
        	echo "Deleted $BUILD";
        fi
    done
}

deployCmd() {
	BUILD_LINE=$(aws gamelift upload-build --name Z8 --build-version 1.0.0 --build-root $Z8_HOME/ServerBuild --operating-system AMAZON_LINUX | tail -1);
    BUILD_ID=$(echo ${BUILD_LINE:10:42});
	echo "Build $BUILD_ID Uploaded Successfully";
	STATUS=$(aws gamelift describe-build --build-id $BUILD_ID  --query "Build.Status" --output text);
	if [[ $STATUS = "READY"* ]]; then
	    echo "Build Ready, Creating Fleet...";
	    RESULT=$(aws gamelift create-fleet --name "Z8_App" --description "Z8 App Server" --build-id $BUILD_ID --ec2-instance-type "c4.large" --runtime-configuration "GameSessionActivationTimeoutSeconds=300, MaxConcurrentGameSessionActivations=2, ServerProcesses=[{LaunchPath=/local/game/App.x86_64, ConcurrentExecutions=1}]" --new-game-session-protection-policy "FullProtection" --resource-creation-limit-policy "NewGameSessionsPerCreator=3,PolicyPeriodInMinutes=15" --ec2-inbound-permissions "FromPort=12345,ToPort=12345,IpRange=0.0.0.0/0,Protocol=UDP" --query "FleetAttributes.Status" --output text);
	    if [[ $RESULT = "NEW"* ]]; then
		    echo "Fleet Created!";
		    NEW_FLEET_ID=$(aws gamelift describe-fleet-attributes --query "FleetAttributes[?BuildId=='$BUILD_ID'].FleetId" --output text);
			echo "New Fleet: ${NEW_FLEET_ID%?}";
			fleetCmd ${NEW_FLEET_ID%?};
		else
		    echo $RESULT;
			echo "Fleet failed to be created";
		fi
    else
		echo "Build $BUILD_ID Not Ready, STATUS=$STATUS";
	fi 
}

FLEET_STATUS="";
fleetCmd(){
	NEW_FLEET_STATUS=$(aws gamelift describe-fleet-attributes --fleet-id $1 --query "FleetAttributes[0].Status" --output text);
	if [[ $NEW_FLEET_STATUS = "ACTIVE"* ]]; then
		echo "Fleet Active!"
		FLEET_STATUS="";
	    IP_ADRESS=$(aws gamelift describe-instances --fleet-id $1 --query "Instances[0].IpAddress" --output text);
		echo "Fleet $1 running on ${IP_ADRESS%?}";
	elif [[ $NEW_FLEET_STATUS = $FLEET_STATUS ]]; then
		fleetCmd;
	else
		FLEET_STATUS=NEW_FLEET_STATUS;
		echo "Status: $FLEET_STATUS";
		fleetCmd;
	fi
}

openCmd() {
    unity -projectPath $Z8_HOME;
}

serverCmd() {
	unity -quit -batchmode -nographics -buildLinux64Player $Z8_HOME/ServerBuild/App.x86_64 -projectPath $Z8_HOME; 
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
elif [[ $1 = "deploy" ]]; then
    deployCmd;
elif [[ $1 = "fleet" ]]; then
    fleetCmd;
elif [[ $1 = "open" ]]; then
    openCmd;
elif [[ $1 = "server" ]]; then
    serverCmd;
elif [[ $1 = "" ]]; then
    noCmd;
else
    invalidCmd $1;
fi