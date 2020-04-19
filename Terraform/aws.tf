terraform {
    backend "remote" {
        hostname = "app.terraform.io"
        organization = "Warshop"
        workspaces {
            prefix = "app-"
        }
    }
}

provider "aws" {
    region = "us-east-1"
}

locals {
    lambdas = [
        "games/get",
        "game/post",
        "join/post"
    ]

    function_names = [
        for lambda in local.lambdas: replace(title(replace(lambda, "/", " ")), " ","")
    ]
}

data "aws_iam_policy_document" "instance_assume_role_policy" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

data "aws_iam_policy_document" "lambda_execution_policy" {
  statement {
    actions = [
      "logs:CreateLogGroup",
      "logs:CreateLogStream",
      "logs:PutLogEvents",
      "gamelift:ListAliases",
      "gamelift:CreateGameSession",
      "gamelift:CreatePlayerSession",
      "gamelift:DescribeAlias",
      "gamelift:DescribeGameSessions"
    ]
    # Replace with game fleet arns from above
    resources = [
      "*"
    ]
  }
}

resource "aws_iam_role" "lambda_role" {
  name = "WarshopLambda"

  assume_role_policy = data.aws_iam_policy_document.instance_assume_role_policy.json
}

resource "aws_iam_policy" "lambda_policy" {
  name        = "WarshopLambdaExecutionPolicy"
  description = "A policy specifying the actions Warshop lambdas are allowed to take"

  policy      = data.aws_iam_policy_document.lambda_execution_policy.json
}

resource "aws_iam_role_policy_attachment" "lambda_attach" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = aws_iam_policy.lambda_policy.arn
}

# lambda resource requires either filename or s3... wow
data "archive_file" "dummy" {
  type        = "zip"
  output_path = "./dummy.zip"

  source {
    content   = "// TODO IMPLEMENT"
    filename  = "dummy.cs"
  }
}

resource "aws_lambda_function" "lambda" {
  for_each      = toset(local.function_names)

  filename      = data.archive_file.dummy.output_path
  function_name = "Warshop${each.value}"
  role          = aws_iam_role.lambda_role.arn
  handler       = "Lambda.WarshopLambda.Handler"

  runtime = "dotnetcore3.1"

  tags = {
    Application = "Warshop"
  }
}
