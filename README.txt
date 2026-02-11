Para ejecutar este programa hay 2 caminos

1: Windows -> cmd

docker build -t pdf-lector-global .

2: 

docker run -d -p 8080:8080 --name mi-lector-pdf pdf-lector-global