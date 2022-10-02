const https = require('https')
const AWS = require('aws-sdk')

exports.handler = async function(event: any) {
  console.log('My Lambda Job');
  console.log(`got env var: ${process.env.MY_SECRET}`);
}