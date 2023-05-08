FROM python:3.9-slim-buster
WORKDIR /app
EXPOSE 80
EXPOSE 5000
EXPOSE 8080
EXPOSE 443

COPY requirements.txt requirements.txt
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

CMD ["gunicorn", "-w", "4", "app:app_with_args", "-b", "0.0.0.0:5000"]