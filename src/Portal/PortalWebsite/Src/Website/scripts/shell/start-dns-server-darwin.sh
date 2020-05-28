CONTAINER_NAME=vscs-local-dns-server

docker_state=$(docker info >/dev/null 2>&1)
if [[ $? -ne 0 ]]; then
    echo "\n** Docker is not running, please start docker deamon first."
    exit 1
fi

# docker is running

echo "\n* Starting the DNS server.."

docker stop $CONTAINER_NAME &> /dev/null
docker rm $CONTAINER_NAME &> /dev/null

echo "* Starting the DNS server docker container.."
docker run --name $CONTAINER_NAME -d --rm \
    --publish 53:53/tcp --publish 53:53/udp \
    vsclkonlinedev.azurecr.io/portal-local-dns:latest

if [ ! "$(docker ps -q -f name=$CONTAINER_NAME)" ]; then
    echo "! Could not start the DNS server docker container, terminating.."
    exit 1
fi

echo "* Adding the DNS server to the Wi-Fi interface DNS servers list.."

networksetup -setdnsservers Wi-Fi 127.0.0.1 $(networksetup -getdnsservers Wi-Fi | grep -v "127\.0\.0\.1")

echo "\n✔ Local DNS server started running at 127.0.0.1:53\n"
