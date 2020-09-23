ARG base_image
FROM ${base_image}
RUN apk add docker -q
