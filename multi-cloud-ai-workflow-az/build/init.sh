export PATH="$PATH:/root/.dotnet/tools"
export NG_CLI_ANALYTICS=ci
git clone https://github.com/ebu/mcma-projects-dotnet.git
mv ./task-inputs.json /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
chmod 777 /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/tasks.sh
rm /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/website/package-lock.json
cd /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az
sed -i "s|evanverneyfinklive|$ONMICROSOFTPREFIX|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/deployment/website/main.tf

sed -i "s|VALUE01|$ENVIRONMENTNAME|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE02|$ENVIRONMENTTYPE|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE03|$AZURESUBSCRIPTIONID|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE04|$AZURETENANTID|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE05|$AZURELOCATION|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE06|$AZURECLIENTID|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE07|$AZURECLIENTSECRET|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE08|$AWSACCESSKEY|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE09|$AWSSECRETKEY|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE10|$AWSREGION|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE11|$AZUREVIDEOINDEXERACCOUNTID|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json
sed -i "s|VALUE12|$AZUREVIDEOINDEXERSUBSCRIPTIONKEY|g" /var/opt/mcma-projects-dotnet/multi-cloud-ai-workflow-az/task-inputs.json

# Deploy
./tasks.sh deploy
