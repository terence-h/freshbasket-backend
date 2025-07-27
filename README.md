# Prerequisites
- S3 Bucket *(for storing product images in Product service only)*
- DynamoDB table(s)
- API Gateway *(basic +proxy setup will do)*
- SNS and SQS *(for queuing order and sending order updates email in Order service only)*
- Depending on your setup, a VPC link with VPC endpoints or NAT gateway
- *(Optional)* Network Load Balancer/Application Load Balancer

# Building/Pushing Docker Image to Amazon ECR
1. Go to Amazon ECR and create a repository
<br>
1.1. **Repository Name**: user-service *(The name will be used in further steps so keep in mind)*
<br>
1.2. *(Optional)* Under **Encryption Settings**, use **AWS KMS** if you wish to encrypt using AWS KMS instead of the default AES-256 encryption.
2. Click on '**Create**'.
3. Open CloudShell *(bottom left of the browser)*
4. Run the script below to clone the git repository to CloudShell.

```
git clone -b develop https://github.com/terence-h/freshbasket-backend.git

# Change 'SERVICE_NAME_HERE' to the service you want to deploy
# Valid services: User.Service, Product.Service, Order.Service
cd freshbasket-backend/User.Service

```
5. In the script provided below, change **ECR_REPO_NAME** to the microservice you. The script will **build the docker image, push the docker image to ECR and clear build images**. This process will take around ~2 minutes.
<br>
```
# Change this to the repository name that was used in step 1.1
export ECR_REPO_NAME=user-service

export AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
export AWS_REGION=us-east-1
export ECR_URI=$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPO_NAME

docker build -t $ECR_REPO_NAME .

aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_URI

docker tag $ECR_REPO_NAME:latest $ECR_URI:latest
docker push $ECR_URI:latest

# Clear build images to prevent capping on storage limitations in CloudShell
docker system prune -f
docker image prune -a -f
docker builder prune -f

```

6. Once it is done, click on the repository name and click on **Copy URI**. You will need the URI for the next portion.
# Creating an ECS task definition
1. Go to Amazon Elastic Container Service, then go to **Task definitions**.
2. Click on '**Create new task definition**'.
<br>
2.1. **Task definition family**: user-service-task *(change based on the service)*
<br>
2.2. **Launch type**: AWS Fargate
<br>
2.2. **CPU**: .25 vCPU and **Memory**: .5 GB *(You will need to select the both options multiple times to be able to set .25 vCPU and .5GB)*
<br>
2.3. **Task role**: LabRole *(for students)* and **Task execution role**: LabRole *(for students)*
<br>
2.4. **Container details name**: user-service *(change based on the service)*
<br>
2.5. **Port mappings (Container port)**: 8080
<br>
2.6. **Environment variables**:

**For all services**
- AWS_REGION: us-east-1
- ASPNETCORE_ENVIRONMENT: Production
- ASPNETCORE_URLS: http://+:8080

**Only for Product and Order service**
- USER_SERVICE_BASE_URL: YOUR_INTERNAL_ALB_DNS/your_listener_route_to_user_service

**Only for User service**
- DYNAMODB_TABLE_NAME: Users
- JWT_AUDIENCE: UserServiceClients
- JWT_ISSUER: UserService
- JWT_EXPIRY_MINS: 10800
- JWT_SECRET_KEY: b53b62055183181a4ec326f815a9759dd184d8bb3c67fb12c502651f11772499

NOTE: You should generate JWT_SECRET_KEY yourself, a sample key is provided only for testing purposes. If you have Node.js installed locally, you can use the script below in command prompt:
```
node -e "console.log(require('crypto').randomBytes(32).toString('hex'))"
```

**Only for Product service**
- PRODUCTS_TABLE_NAME: Products
- CATEGORIES_TABLE_NAME: Categories
- S3_BUCKET_NAME: YOUR_S3_BUCKET_NAME_TO_STORE_PRODUCT_IMAGES

**Only for Order service**
- DYNAMODB_TABLE_NAME: Orders
- SQS_QUEUE_URL: YOUR_SQS_QUEUE_URL
- SNS_QUEUE_URL: YOUR_SNS_QUEUE_URL
- SNS_TOPIC_ARN: YOUR_SNS_TOPIC_ARN

3. Click on '**Create**'.

# Creating a ECS Cluster
1. On the left menu, click on **Clusters** then **Create cluster**.
2. **Cluster name**: freshbasket *(or any desired name)*.
3. Under Infrastructure, use **AWS Fargate (serverless)**.
4. Under Monitoring, select **Container Insights with enhanced observability** to get more detailed health and performance metrics.
5. *(Optional)* Under Encryption, use an AWS KMS key to encrypt the storage.
6. Click on '**Create**'.

# Running the Service using the Task Definition
1. On the left menu, select **Task definitions**.
2. Select the task definition you want to deploy and click on **Deploy** then **Create service**.
<br>
2.1. **Task definition revision**: Select the one with the (LATEST) name
<br>
2.2. **Service name**: ANY_NAME
<br>
2.3. **Existing cluster**: The cluster that was just created.
<br>
2.4. **Compute options**: Launch type, leave the rest default.
<br>
2.5. **Desired tasks**: Set the value to how many availability zones you have.
<br>
2.6. *(Optional)* **Load balancing**: Set it to your internal ALB.
<br>
2.7. *(Optional)* Under **Service auto-scaling**:
- **Minimum number of task**: 1
- **Maximum number of tasks**: Set to the number of multi-AZ you have.
- **Policy name**: Any name
- **ECS service metric**: ECSServiceAverageCPUUtilization
- **Target value**: 75
- **Scale-out cooldown period**: 300
- **Scale-in cooldown period**: 300
<br><br>
2.8. Click on '**Create**'.

The deployment process will take about ~2 minutes.

Once the deployment is completed, you should be able to access the microservice endpoints through an Amazon API Gateway.