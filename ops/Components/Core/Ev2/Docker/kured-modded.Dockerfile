ARG base_image=docker.io/weaveworks/kured@sha256:91335d3a1215eb768a440bf2b6779bd4ec2023ff2251f4b7af371f1e5ce65bdb
FROM ${base_image}
# Updates kubectl to 1.14.6.
ADD https://storage.googleapis.com/kubernetes-release/release/v1.14.6/bin/linux/amd64/kubectl /usr/bin/kubectl
RUN chmod 0755 /usr/bin/kubectl
