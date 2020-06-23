FROM ubuntu as certificates

RUN apt-get update && apt-get install openssl -y
WORKDIR /certificates

ADD ["dev-cert.pfx", "dev-github-cert.pfx", "local-github-dev.pfx", "dev-github-pf-cert.pfx", "dev-github-codespaces-pf-cert.pfx", "./"]

# dev-cert
RUN openssl pkcs12 -in ./dev-cert.pfx -clcerts -nokeys -out dev-cert.crt -passin pass: 
RUN openssl pkcs12 -in ./dev-cert.pfx -nocerts -nodes -out dev-cert.rsa -passin pass: 
# dev-github-cert
RUN  openssl pkcs12 -in ./dev-github-cert.pfx -clcerts -nokeys -out dev-github-cert.crt -passin pass: 
RUN openssl pkcs12 -in ./dev-github-cert.pfx -nocerts -nodes -out dev-github-cert.rsa -passin pass: 
# dev-github-pf-cert
RUN  openssl pkcs12 -in ./dev-github-pf-cert.pfx -clcerts -nokeys -out dev-github-pf-cert.crt -passin pass: 
RUN openssl pkcs12 -in ./dev-github-pf-cert.pfx -nocerts -nodes -out dev-github-pf-cert.rsa -passin pass: 
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

COPY --from=certificates /certificates/* /certificates/

ADD templates/*.template /etc/nginx/templates/