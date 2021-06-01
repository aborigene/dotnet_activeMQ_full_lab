#
# Build stage
#
FROM maven:3.6.3-openjdk-11-slim AS build
COPY src /home/app/src/
COPY pom.xml /home/app/
WORKDIR /home/app/
RUN mvn clean package -DskipTests

#
# Package stage
#
FROM openjdk:11.0.11-jre
COPY --from=build /home/app/target/pix_processor-0.0.1-SNAPSHOT.jar /usr/local/lib/pix_processor-0.0.1-SNAPSHOT.jar
ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh /app/
RUN chmod 755 /app/wait-for-it.sh
ENTRYPOINT ["wait-for-it.sh", "127.0.0.1:61616", "--", "wait-for-it.sh", "127.0.0.1:3306", "--", "java","-jar","/usr/local/lib/pix_processor-0.0.1-SNAPSHOT.jar"]