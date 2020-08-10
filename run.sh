 docker build -t s3proxy .

 docker run -d -p 80:80 -p 443:443 --name myproxy --env-file=.env s3proxy