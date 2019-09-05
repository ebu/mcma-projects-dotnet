resource "aws_s3_bucket_object" "file_0" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "3rdpartylicenses.txt"
  source       = "../website/dist/website/3rdpartylicenses.txt"
  content_type = "text/plain"
  etag         = "${filemd5("../website/dist/website/3rdpartylicenses.txt")}"
}

resource "aws_s3_bucket_object" "file_1" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "config.json"
  source       = "../website/dist/website/config.json"
  content_type = "application/json"
  etag         = "${filemd5("../website/dist/website/config.json")}"
}

resource "aws_s3_bucket_object" "file_2" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "favicon.ico"
  source       = "../website/dist/website/favicon.ico"
  content_type = "image/x-icon"
  etag         = "${filemd5("../website/dist/website/favicon.ico")}"
}

resource "aws_s3_bucket_object" "file_3" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "index.html"
  source       = "../website/dist/website/index.html"
  content_type = "text/html"
  etag         = "${filemd5("../website/dist/website/index.html")}"
}

resource "aws_s3_bucket_object" "file_4" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "main.709d571198474daedfb7.js"
  source       = "../website/dist/website/main.709d571198474daedfb7.js"
  content_type = "application/javascript"
  etag         = "${filemd5("../website/dist/website/main.709d571198474daedfb7.js")}"
}

resource "aws_s3_bucket_object" "file_5" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "polyfills.32c5648fd4ca0eb849e2.js"
  source       = "../website/dist/website/polyfills.32c5648fd4ca0eb849e2.js"
  content_type = "application/javascript"
  etag         = "${filemd5("../website/dist/website/polyfills.32c5648fd4ca0eb849e2.js")}"
}

resource "aws_s3_bucket_object" "file_6" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "runtime.26209474bfa8dc87a77c.js"
  source       = "../website/dist/website/runtime.26209474bfa8dc87a77c.js"
  content_type = "application/javascript"
  etag         = "${filemd5("../website/dist/website/runtime.26209474bfa8dc87a77c.js")}"
}

resource "aws_s3_bucket_object" "file_7" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "styles.b3b9a8b9ca51bd14285c.css"
  source       = "../website/dist/website/styles.b3b9a8b9ca51bd14285c.css"
  content_type = "text/css"
  etag         = "${filemd5("../website/dist/website/styles.b3b9a8b9ca51bd14285c.css")}"
}

resource "aws_s3_bucket_object" "file_8" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "assets/images/cloud-logo.svg"
  source       = "../website/dist/website/assets/images/cloud-logo.svg"
  content_type = "image/svg+xml"
  etag         = "${filemd5("../website/dist/website/assets/images/cloud-logo.svg")}"
}

