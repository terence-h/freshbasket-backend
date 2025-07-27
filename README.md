# Prerequisites
- S3 Bucket *(for storing product images in Product service only)*
- DynamoDB table(s)
- API Gateway *(basic +proxy setup will do)*
- SNS and SQS *(for queuing order and sending order updates email in Order service only)*
- Depending on your setup, a VPC link with VPC endpoints or NAT gateway
- *(Optional)* Network Load Balancer/Application Load Balancer

# Building/Pushing Docker Image to Amazon ECR
1. Go to Amazon ECR and create a repository
2. Use the following settings *(if not mentioned, leave as default)*:
    | Option | Value |
    |---|---|
    | Repository Name | user-service *(The name will be used in further steps so keep in mind)* |
    | *(Optional)* Encryption Settings | AWS KMS or leave as default |
4. Click on '**Create**'.
5. Open CloudShell *(bottom left of the browser)*
6. Run the script below to clone the git repository to CloudShell.

```
git clone -b develop https://github.com/terence-h/freshbasket-backend.git

# Valid services: User.Service, Product.Service, Order.Service
cd freshbasket-backend/User.Service

```
7. In the script provided below, change **ECR_REPO_NAME** to the microservice you. The script will **build the docker image, push the docker image to ECR and clear build images**. This process will take around ~2 minutes.
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
8. Once it is done, click on the repository name and click on **Copy URI**. You will need the URI for the next portion.

# Creating an ECS task definition
1. Go to Amazon Elastic Container Service, then go to **Task definitions**.
2. Click on '**Create new task definition**'.
3. Use the following settings *(if not mentioned, leave as default)*:
    | Option | Value |
    |---|---|
    | Task definition family | user-service-task *(change based on the service)* |
    | Launch type | AWS Fargate |
    | CPU | .25 vCPU *(you will need to select CPU then Memory then back to CPU to get .25 vCPU option)* |
    | Memory | .5 GB *(you will need to select CPU then Memory then back to CPU to get .5 GB option)* |
    | Task role | Any role with ECS permissions or LabRole *(for students)* |
    | Task execution role | Any role with ECS task execution or LabRole *(for students)* |
    | Container details name | user-service *(change based on the service)* |
    | Port mappings (Container port) | 8080 |
    | Launch type | AWS Fargate |

    **Environment Variables**:

    **For all services**
    | Key | Value |
    |---|---|
    | AWS_REGION | us-east-1 |
    | ASPNETCORE_ENVIRONMENT | Production |
    | ASPNETCORE_URLS | http://+:8080 |

    **Only for Product and Order service**
    | Key | Value |
    |---|---|
    | USER_SERVICE_BASE_URL | YOUR_INTERNAL_ALB_DNS/your_listener_route_to_user_service |

    **Only for User service**
    | Key | Value |
    |---|---|
    | DYNAMODB_TABLE_NAME | Users |
    | JWT_AUDIENCE | UserServiceClients |
    | JWT_ISSUER | UserService |
    | JWT_EXPIRY_MINS | 10800 |
    | JWT_SECRET_KEY | b53b62055183181a4ec326f815a9759dd184d8bb3c67fb12c502651f11772499 |

    NOTE: You should generate JWT_SECRET_KEY yourself, a sample key is provided only for testing purposes. If you have Node.js installed locally, you can use the script below in command prompt:
    ```
    node -e "console.log(require('crypto').randomBytes(32).toString('hex'))"
    ```
    **Only for Product service**
    | Key | Value |
    |---|---|
    | PRODUCTS_TABLE_NAME | Products |
    | CATEGORIES_TABLE_NAME | Categories |
    | S3_BUCKET_NAME | YOUR_S3_BUCKET_NAME_TO_STORE_PRODUCT_IMAGES |

    **Only for Order service**
    | Key | Value |
    |---|---|
    | DYNAMODB_TABLE_NAME | Orders |
    | SQS_QUEUE_URL | YOUR_SQS_QUEUE_URL |
    | SNS_QUEUE_URL | YOUR_SNS_QUEUE_URL |
    | SNS_TOPIC_ARN | YOUR_SNS_TOPIC_ARN |

3. Click on '**Create**'.

# Creating a ECS Cluster
1. On the left menu, click on **Clusters** then **Create cluster**.
2. Use the following settings *(if not mentioned, leave as default)*:
    | Option | Value |
    |---|---|
    | Cluster name | freshbasket *(or any desired name)* |
    | In Infrastructure | AWS Fargate (serverless) |
    | In Monitoring | Container Insights with enhanced observability |
    | *(Optional)* In Encryption | AWS KMS |
3. Click on '**Create**'.

# Running the Service using the Task Definition created
1. On the left menu, select **Task definitions**.
2. Select the task definition you want to deploy and click on **Deploy** then **Create service**.
3. Use the following settings *(if not mentioned, leave as default)*:
    | Option | Value |
    |---|---|
    | Task definition revision | Select the one with the (LATEST) name |
    | Service name | Any name |
    | Existing cluster | The cluster that was just created. |
    | Compute options | Launch type |
    | Desired tasks | Set the value to how many availability zones you have. |
    | *(Optional)* Load balancing | Set it to your internal ALB. |
    
    ***(Optional)* In Service auto-scaling**
    | Option | Value |
    |---|---|
    | Minimum number of tasks | 1 |
    | Maximum number of tasks | Set to the number of multi-AZ you have. |
    | Policy name | Any name |
    | ECS service metric | ECSServiceAverageCPUUtilization |
    | Target value | 70 |
    | Scale-out cooldown period | 300 |
    | Scale-in cooldown period | 300 |
4. Click on '**Create**'.

The deployment process will take about ~2 minutes.

Once the deployment is completed, you should be able to access the microservice endpoints through an Amazon API Gateway.