#!/bin/sh

helpCmd() {
    echo "You could use the following commands:";
    echo "    help: prints all available commands to console";
	echo "    deploy: deploys the app server to aws";
	echo "    fleet: describes the fleet instances"
	echo "    open: opens the unity project and vs";
	echo "    server: builds the app server";
}

deployCmd() {
	BUILD_LINE=$(aws gamelift upload-build --name Z8 --build-version 1.0.0 --build-root $Z8_HOME/ServerBuild --operating-system AMAZON_LINUX | tail -1);
    BUILD_ID=$(echo ${BUILD_LINE:10:42});
	echo "Build $BUILD_ID Uploaded Successfully";
	STATUS_LINE=$(aws gamelift describe-build --build-id $BUILD_ID | head -n 3 | tail -1);
    STATUS=$(echo ${STATUS_LINE:19:5});
	if [[ $STATUS = "READY" ]]; then
	    echo "Build Ready, Creating Fleet...";
	    RESULT=$(aws gamelift create-fleet --name "Z8_App" --description "Z8 App Server" --build-id $BUILD_ID --ec2-instance-type "c4.large" --runtime-configuration "GameSessionActivationTimeoutSeconds=300, MaxConcurrentGameSessionActivations=2, ServerProcesses=[{LaunchPath=/local/game/App.x86_64, ConcurrentExecutions=1}]" --new-game-session-protection-policy "FullProtection" --resource-creation-limit-policy "NewGameSessionsPerCreator=3,PolicyPeriodInMinutes=15" --ec2-inbound-permissions "FromPort=12345,ToPort=12345,IpRange=0.0.0.0/0,Protocol=UDP");
		RESULT_STATUS_LINE=$(echo $RESULT | head -n 3 | tail -1);
		RESULT_STATUS=$(echo ${RESULT_STATUS_LINE:19:3});
	    if [[ $STATUS = "NEW" ]]; then
		    echo "Fleet Created! Done.";
		else
		    echo $RESULT;
			echo "Fleet failed to be created";
		fi
    else
		echo "Build Not Ready, STATUS=$STATUS";
	fi 
}

fleetCmd() {
	aws gamelift describe-instances --fleet-id $Z8_FLEET_ID --limit 1
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
elif [[ $1 = "deploy" ]]; then
    deployCmd;
elif [[ $1 = "open" ]]; then
    openCmd;
elif [[ $1 = "fleet" ]]; then
    fleetCmd;
elif [[ $1 = "server" ]]; then
    serverCmd;
elif [[ $1 = "" ]]; then
    noCmd;
else
    invalidCmd $1;
fi