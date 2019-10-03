FROM python:3-slim

ADD requirements.txt .
RUN pip install -r requirements.txt

ADD app.py .

ENTRYPOINT [ "/usr/local/bin/python3", "-u", "app.py" ]
