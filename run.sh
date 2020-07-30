 docker build -t s3proxy .

 docker run -d -p 80:80 --name myproxy --env-file=.env s3proxy