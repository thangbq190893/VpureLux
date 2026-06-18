docker-compose -f containers/rabbitmq.yml down
docker-compose -f containers/redis.yml down
exit $LASTEXITCODE
