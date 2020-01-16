#===================================================================
# AWS SNS Topic, Subscription, and S3 Bucket Notifications
#===================================================================

provider "aws" {
  version = "~> 2.0"
  access_key = var.aws_access_key
  secret_key = var.aws_secret_key
  region = var.aws_region
}

resource "aws_s3_bucket" "aws_ai_input_bucket" {
  bucket = "${var.global_prefix}-${var.aws_region}-aws-ai-input"
  acl    = "private"
  force_destroy = true
}

resource "aws_s3_bucket" "aws_ai_output_bucket" {
  bucket = "${var.global_prefix}-${var.aws_region}-aws-ai-output"
  acl    = "private"
  force_destroy = true
}

resource "aws_sns_topic" "aws_ai_output_topic" {
  name = "${var.global_prefix}-${var.aws_region}-aws-ai-output"
  
  policy = <<POLICY
  {
    "Version": "2012-10-17",
    "Statement": [
      {
        "Sid": "s3bucket",
        "Effect": "Allow",
        "Principal": {
          "Service": "s3.amazonaws.com"
        },
        "Action": "sns:Publish",
        "Resource": "arn:aws:sns:*:*:${var.global_prefix}-${var.aws_region}-aws-ai-output",
        "Condition": {
            "ArnLike": { "aws:SourceArn": "${aws_s3_bucket.aws_ai_output_bucket.arn}" }
        }
      },
      {
        "Sid": "rekognition",
        "Effect": "Allow",
        "Principal": {
          "Service": "rekognition.amazonaws.com"
        },
        "Action": "sns:Publish",
        "Resource": "arn:aws:sns:*:*:${var.global_prefix}-${var.aws_region}-aws-ai-output"
      }
    ]
  }
  POLICY
}

resource "aws_iam_role" "aws_reko_sns_role" {
  name = "${var.global_prefix}-${var.aws_region}-reko-sns-role"

  assume_role_policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "rekosns",
      "Effect": "Allow",
      "Principal": {
        "Service": "rekognition.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
EOF
}

resource "aws_iam_role_policy" "aws_reko_sns_role_policy" {
  name = "${var.global_prefix}-${var.aws_region}-reko-sns-role-policy"
  role = aws_iam_role.aws_reko_sns_role.id

  policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Action": [
        "sns:Publish"
      ],
      "Effect": "Allow",
      "Resource": "*"
    }
  ]
}
  EOF
}

resource "aws_s3_bucket_notification" "aws_ai_output_bucket_notification" {
  bucket = aws_s3_bucket.aws_ai_output_bucket.id

  topic {
    topic_arn     = aws_sns_topic.aws_ai_output_topic.arn
    events        = ["s3:ObjectCreated:*"]
  }
}

resource "aws_sns_topic_subscription" "aws_sns_topic_sub_lambda" {
  topic_arn = aws_sns_topic.aws_ai_output_topic.arn
  protocol = "https"
  endpoint = "https://${azurerm_function_app.aws_ai_service_sns_function.default_hostname}/sns-notifications?code=${local.aws_ai_service_sns_func_key}"
  endpoint_auto_confirms = true
}

resource "aws_iam_user" "aws_ai_user" {
  name = "${var.global_prefix}_aws_ai_user"
}

resource "aws_iam_user_policy" "aws_ai_user_policy" {
  name = "${var.global_prefix}_aws_ai_user_policy"
  user = aws_iam_user.aws_ai_user.name

  policy = <<POLICY
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "s3buckets",
      "Effect": "Allow",
      "Action": "s3:GetBucketLocation",
      "Resource": "arn:aws:s3:::${var.global_prefix}-${var.aws_region}-aws-ai-*put"
    },
    {
      "Sid": "s3objects",
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject",
        "s3:DeleteObject"
      ],
      "Resource": "arn:aws:s3:::${var.global_prefix}-${var.aws_region}-aws-ai-*put/*"
    },
    {
      "Sid": "aiservices",
      "Effect": "Allow",
      "Action": [
        "rekognition:StartCelebrityRecognition",
        "rekognition:GetCelebrityRecognition",
        "transcribe:StartTranscriptionJob",
        "translate:TranslateText",
        "comprehend:DetectDominantLanguage"
      ],
      "Resource": "*"
    },
    {
      "Sid": "iam",
      "Effect": "Allow",
      "Action": "iam:PassRole",
      "Resource": "${aws_iam_role.aws_reko_sns_role.arn}"
    }
  ]
}
  POLICY
}

resource "aws_iam_access_key" "aws_ai_user_access_key" {
  user    = aws_iam_user.aws_ai_user.name
}