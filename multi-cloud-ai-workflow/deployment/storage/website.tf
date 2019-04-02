resource "aws_s3_bucket_object" "file_0" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "3rdpartylicenses.txt"
  source       = "../website/dist/website/3rdpartylicenses.txt"
  content_type = "text/plain"
  etag         = "${md5(file("../website/dist/website/3rdpartylicenses.txt"))}"
}

resource "aws_s3_bucket_object" "file_1" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "config.json"
  source       = "../website/dist/website/config.json"
  content_type = "application/json"
  etag         = "${md5(file("../website/dist/website/config.json"))}"
}

resource "aws_s3_bucket_object" "file_2" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "favicon.ico"
  source       = "../website/dist/website/favicon.ico"
  content_type = "image/x-icon"
  etag         = "${md5(file("../website/dist/website/favicon.ico"))}"
}

resource "aws_s3_bucket_object" "file_3" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "index.html"
  source       = "../website/dist/website/index.html"
  content_type = "text/html"
  etag         = "${md5(file("../website/dist/website/index.html"))}"
}

resource "aws_s3_bucket_object" "file_4" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "main.9f7e212068f5d697d6a7.js"
  source       = "../website/dist/website/main.9f7e212068f5d697d6a7.js"
  content_type = "application/javascript"
  etag         = "${md5(file("../website/dist/website/main.9f7e212068f5d697d6a7.js"))}"
}

resource "aws_s3_bucket_object" "file_5" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "polyfills.b23caee6469a572f772d.js"
  source       = "../website/dist/website/polyfills.b23caee6469a572f772d.js"
  content_type = "application/javascript"
  etag         = "${md5(file("../website/dist/website/polyfills.b23caee6469a572f772d.js"))}"
}

resource "aws_s3_bucket_object" "file_6" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "runtime.a5dd35324ddfd942bef1.js"
  source       = "../website/dist/website/runtime.a5dd35324ddfd942bef1.js"
  content_type = "application/javascript"
  etag         = "${md5(file("../website/dist/website/runtime.a5dd35324ddfd942bef1.js"))}"
}

resource "aws_s3_bucket_object" "file_7" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "styles.3dc2d43a14eaab0ed152.css"
  source       = "../website/dist/website/styles.3dc2d43a14eaab0ed152.css"
  content_type = "text/css"
  etag         = "${md5(file("../website/dist/website/styles.3dc2d43a14eaab0ed152.css"))}"
}

resource "aws_s3_bucket_object" "file_8" {
  bucket       = "${aws_s3_bucket.website.bucket}"
  key          = "assets/images/cloud-logo.svg"
  source       = "../website/dist/website/assets/images/cloud-logo.svg"
  content_type = "image/svg+xml"
  etag         = "${md5(file("../website/dist/website/assets/images/cloud-logo.svg"))}"
}

