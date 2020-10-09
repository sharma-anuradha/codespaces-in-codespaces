FROM ubuntu as certificates

RUN apt-get update && apt-get install openssl -y
WORKDIR /certificates

ADD ["dev-cert.pfx", "local-github-dev.pfx", "dev-github-codespaces-pf-cert.pfx", "./"]

# dev-cert
RUN openssl pkcs12 -in ./dev-cert.pfx -clcerts -nokeys -out dev-cert.crt -passin pass: 
RUN openssl pkcs12 -in ./dev-cert.pfx -nocerts -nodes -out dev-cert.rsa -passin pass: 
# dev-github-codespaces-pf-cert
RUN  openssl pkcs12 -in ./dev-github-codespaces-pf-cert.pfx -clcerts -nokeys -out dev-github-codespaces-pf-cert.crt -passin pass: 
RUN openssl pkcs12 -in ./dev-github-codespaces-pf-cert.pfx -nocerts -nodes -out dev-github-codespaces-pf-cert.rsa -passin pass: 
# local-github-dev
RUN  openssl pkcs12 -in ./local-github-dev.pfx -clcerts -nokeys -out local-github-dev.crt -passin pass: 
RUN openssl pkcs12 -in ./local-github-dev.pfx -nocerts -nodes -out local-github-dev.rsa -passin pass: 

FROM nginx

EXPOSE 443

ENV PORTAL_ORIGIN http://host.docker.internal:5000
ENV PORT_FORWARDING_ORIGIN http://host.docker.internal:5000
ENV API_ORIGIN http://vsclk-online-dev-ci-tm.trafficmanager.net

COPY --from=certificates /certificates/* /certificates/

ADD templates/*.template /etc/nginx/templates/