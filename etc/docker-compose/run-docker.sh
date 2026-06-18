#!/bin/bash

if [[ ! -d certs ]]
then
    mkdir certs
    cd certs/
    if [[ ! -f localhost.pfx ]]
    then
        dotnet dev-certs https -v -ep localhost.pfx -p 4f8614ec-1ec5-405f-a2c0-baa4ba612397 -t
    fi
    cd ../
fi

docker-compose up -d
