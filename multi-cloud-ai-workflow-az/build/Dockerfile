FROM ubuntu:19.10

RUN apt-get update
RUN apt-get install curl -y
RUN curl -o msft.deb https://packages.microsoft.com/config/ubuntu/19.04/packages-microsoft-prod.deb
RUN dpkg -i msft.deb

RUN apt-get update

RUN apt-get install nano git unzip default-jre python2.7 python-pip apt-transport-https dotnet-sdk-2.1 dotnet-sdk-3.1 -y

RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash

# get install script and pass it to execute: 
RUN curl -sL https://deb.nodesource.com/setup_10.x | bash -
# and install node 
RUN apt-get update
RUN apt-get install nodejs -y
# confirm that it was successful 
RUN node -v
# npm installs automatically 
RUN npm -v

RUN curl -o tf.zip https://releases.hashicorp.com/terraform/0.12.12/terraform_0.12.12_linux_amd64.zip
RUN unzip tf.zip
RUN mv terraform /usr/local/bin
RUN rm tf.zip
RUN dotnet tool install -g dotnet-script


WORKDIR /var/opt
COPY ./task-inputs.json .
COPY ./init.sh .
