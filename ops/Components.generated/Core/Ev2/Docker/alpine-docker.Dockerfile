ARG base_image=docker.io/library/alpine:3.10.3
FROM ${base_image}
RUN apk add docker -q
