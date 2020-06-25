FROM coredns/coredns:latest

ARG CONFIGS_DIR
WORKDIR /dns-config
ADD ${CONFIGS_DIR:-.}/* ./

ENTRYPOINT [ "/coredns" ]