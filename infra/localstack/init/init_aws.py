import os

import boto3
from botocore.exceptions import ClientError

BUCKET = "photo-gallery-local"
TABLE = "PhotoGallery"
QUEUE = "photo-gallery-processing"
ENDPOINT = "http://localhost:4566"
REGION = os.environ.get("AWS_DEFAULT_REGION", "eu-central-1")

session = boto3.Session(
    aws_access_key_id="test",
    aws_secret_access_key="test",
    region_name=REGION,
)
s3 = session.client("s3", endpoint_url=ENDPOINT)
dynamodb = session.client("dynamodb", endpoint_url=ENDPOINT)
sqs = session.client("sqs", endpoint_url=ENDPOINT)


def create_bucket() -> None:
    try:
        s3.create_bucket(
            Bucket=BUCKET,
            CreateBucketConfiguration={"LocationConstraint": REGION},
        )
        print(f"bucket {BUCKET} created")
    except ClientError as e:
        if e.response["Error"]["Code"] != "BucketAlreadyOwnedByYou":
            raise
        print(f"bucket {BUCKET} already exists")

    s3.put_bucket_cors(
        Bucket=BUCKET,
        CORSConfiguration={
            "CORSRules": [
                {
                    "AllowedOrigins": ["http://localhost:3000"],
                    "AllowedMethods": ["POST", "GET"],
                    "AllowedHeaders": ["*"],
                    "ExposeHeaders": ["ETag"],
                    "MaxAgeSeconds": 3000,
                }
            ]
        },
    )


def create_table() -> None:
    try:
        dynamodb.create_table(
            TableName=TABLE,
            BillingMode="PAY_PER_REQUEST",
            AttributeDefinitions=[
                {"AttributeName": "PK", "AttributeType": "S"},
                {"AttributeName": "SK", "AttributeType": "S"},
                {"AttributeName": "GSI1PK", "AttributeType": "S"},
                {"AttributeName": "GSI1SK", "AttributeType": "S"},
            ],
            KeySchema=[
                {"AttributeName": "PK", "KeyType": "HASH"},
                {"AttributeName": "SK", "KeyType": "RANGE"},
            ],
            GlobalSecondaryIndexes=[
                {
                    "IndexName": "GSI1",
                    "KeySchema": [
                        {"AttributeName": "GSI1PK", "KeyType": "HASH"},
                        {"AttributeName": "GSI1SK", "KeyType": "RANGE"},
                    ],
                    "Projection": {"ProjectionType": "ALL"},
                }
            ],
        )
        print(f"table {TABLE} created")
        dynamodb.update_time_to_live(
            TableName=TABLE,
            TimeToLiveSpecification={"Enabled": True, "AttributeName": "ExpiresAt"},
        )
    except ClientError as e:
        if e.response["Error"]["Code"] != "ResourceInUseException":
            raise
        print(f"table {TABLE} already exists")


def create_processing_queue() -> None:
    """Local stand-in for the S3 -> Lambda trigger: S3 notifies SQS, tools/PhotoGallery.LocalProcessor consumes it."""
    queue_url = sqs.create_queue(QueueName=QUEUE)["QueueUrl"]
    queue_arn = sqs.get_queue_attributes(QueueUrl=queue_url, AttributeNames=["QueueArn"])["Attributes"]["QueueArn"]

    s3.put_bucket_notification_configuration(
        Bucket=BUCKET,
        NotificationConfiguration={
            "QueueConfigurations": [
                {
                    "QueueArn": queue_arn,
                    "Events": ["s3:ObjectCreated:*"],
                    "Filter": {"Key": {"FilterRules": [{"Name": "prefix", "Value": "originals/"}]}},
                }
            ]
        },
    )
    print(f"queue {QUEUE} receives S3 originals/ events")


create_bucket()
create_table()
create_processing_queue()
print("photo-gallery init done")