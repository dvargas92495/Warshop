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

    function_names = {
        for lambda in local.lambdas: 
        lambda => replace(title(replace(lambda, "/", " ")), " ","")
    }

    function_handlers = {
        for lambda in local.lambdas: 
        lambda => replace(title(replace(lambda, "/", " ")), " ","::")
    }
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

data "aws_iam_policy_document" "gamelift_assume_role_policy" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["gamelift.amazonaws.com"]
    }
  }
}

resource "aws_s3_bucket" "gamelift_builds" {
  bucket = "warshop-gamelift-builds"
  acl    = "private"

  tags = {
    Application = "Warshop"
  }
}

resource "aws_s3_bucket_object" "gamelift_build" {
  key        = "warshop_build"
  bucket     = aws_s3_bucket.gamelift_builds.id
  source     = "../server.zip"
}

data "aws_iam_policy_document" "gamelift_build_policy" {
  statement {
    actions = [
      "s3:PutObject",
      "s3:GetObject",
      "s3:DeleteObject",
      "s3:ListBucket",
    ]
    resources = [
      aws_s3_bucket.gamelift_builds.arn,
      "${aws_s3_bucket.gamelift_builds.arn}/*"
    ]
  }
}

resource "aws_iam_role" "gamelift_role" {
  name = "WarshopGamelift"

  assume_role_policy = data.aws_iam_policy_document.gamelift_assume_role_policy.json

  tags = {
    Application = "Warshop"
  }
}

resource "aws_iam_policy" "gamelift_build_policy" {
  name        = "WarshopGameliftBuilds"
  description = "A policy specifying the actions Warshop lambdas are allowed to take"

  policy      = data.aws_iam_policy_document.gamelift_build_policy.json
}

resource "aws_iam_role_policy_attachment" "gamelift_build_attach" {
  role       = aws_iam_role.gamelift_role.name
  policy_arn = aws_iam_policy.gamelift_build_policy.arn
}

resource "aws_gamelift_build" "build" {
  name             = "Warshop"
  operating_system = "WINDOWS_2012"

  storage_location {
    bucket   = aws_s3_bucket.gamelift_builds.bucket
    key      = aws_s3_bucket_object.gamelift_build.key
    role_arn = aws_iam_role.gamelift_role.arn
  }

  tags = {
    Application = "Warshop"
  }

  version    = "2020.113.0"
}

resource "aws_gamelift_fleet" "fleet" {
  build_id          = aws_gamelift_build.build.id
  ec2_instance_type = "c4.large"
  fleet_type        = "ON_DEMAND"
  name              = "Warshop"
  description       = "Warshop App Server"
  new_game_session_protection_policy = "FullProtection"

  runtime_configuration {
    game_session_activation_timeout_seconds = 600
    server_process {
      concurrent_executions = 1
      launch_path           = "C:\\game\\ServerBuild\\App.exe"
    }
  }

  ec2_inbound_permission {
    from_port = 12345
    to_port   = 12345
    ip_range  = "0.0.0.0/0"
    protocol  = "UDP"
  }

  tags = {
    Application = "Warshop"
  }

  timeouts {
    create = "15m"
    delete = "15m"
  }

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_gamelift_alias" "alias" {
  name        = "WarshopServer"
  description = "Alias for the Warshop Server Fleet"

  routing_strategy {
    message = "WarshopServer"
    type    = "TERMINAL"
    fleet_id = aws_gamelift_fleet.fleet.id
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

data "aws_iam_policy_document" "instance_assume_role_policy" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "lambda_role" {
  name = "WarshopLambda"

  assume_role_policy = data.aws_iam_policy_document.instance_assume_role_policy.json

  tags = {
    Application = "Warshop"
  }
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

resource "aws_lambda_function" "lambda" {
  for_each      = toset(local.lambdas)

  filename      = each.value == "games/get" ? "../Lambda/GamesGet/GamesGet.zip" : data.archive_file.dummy.output_path
  function_name = "Warshop${local.function_names[each.value]}"
  role          = aws_iam_role.lambda_role.arn
  handler       = "Lambda.${local.function_handlers[each.value]}"

  runtime = "dotnetcore3.1"

  tags = {
    Application = "Warshop"
  }
}
