ARG base_image
FROM ${base_image}
ADD https://storage.googleapis.com/kubernetes-release/release/v1.18.6/bin/linux/amd64/kubectl /usr/bin/kubectl
RUN chmod 0755 /usr/bin/kubectl
