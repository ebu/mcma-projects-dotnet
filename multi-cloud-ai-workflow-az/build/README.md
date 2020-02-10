# Build and Deploy MCMA for Azure with Docker
## Steps
* Use the Dockerfile in this folder to build a docker image. Use the following `docker` command.
* `docker build -t mcma-deploy-az --rm  . `
* Edit the [__env.txt__](env.txt) with all the required values.
* Run the docker container with this `docker` command
* `docker run --env-file env.txt --name mcma-build -it mcma-deploy-az /bin/bash`
* Once inside the container, run the [__init.sh__](init.sh) script:
* `root@d5ac7e9bca23:/var/opt# ./init.sh`

This script will 
* clone this repository into the Docker container.
* populate the `task-inputs.json` file wit the environment variables that were passed in.
* run __./tasks.sh deploy__ and deploy your MCMA resources to Azure.
