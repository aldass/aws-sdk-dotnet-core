using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Amazon.S3;
using Amazon.S3.Model;

namespace AwsConsoleApp1
{
    public class Program
    {
        static IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "null";
#if DEBUG
            environmentName = "Development";
#endif

            var currDir = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(currDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables();


            Configuration = builder.Build();

            Console.Write(GetServiceOutput());
            Console.Read();
        }

        static string GetServiceOutput()
        {
            var awsOptions = Configuration.GetAWSOptions();
            var awsRegion = awsOptions.Region.SystemName;
            StringBuilder sb = new StringBuilder(1024);
            using (StringWriter sr = new StringWriter(sb))
            {
                sr.WriteLine("===========================================");
                sr.WriteLine("AWS SDK with DotNet Core 2.x -- Al Dass (AL.Dass@gmail.com)");
                sr.WriteLine("===========================================");

                // Print the number of Amazon EC2 instances.
                IAmazonEC2 ec2 = awsOptions.CreateServiceClient<IAmazonEC2>();
                DescribeInstancesRequest ec2Request = new DescribeInstancesRequest();

                try
                {

                    DescribeInstancesResponse ec2Response = ec2.DescribeInstancesAsync(ec2Request).Result;
                    int numInstances = 0;
                    numInstances = ec2Response.Reservations.Count;
                    sr.WriteLine($"You have {numInstances} Amazon EC2 instance(s) in the [{awsRegion}] region.");

                    foreach (var r in ec2Response.Reservations)
                    {
                        foreach (var i in r.Instances)
                        {
                            sr.WriteLine("===========================================");
                            sr.WriteLine($"        InstanceId: {i.InstanceId}");
                            sr.WriteLine($"             VpcId: {i.VpcId}");
                            sr.WriteLine($"     Current State: {i.State?.Name?.Value}");
                            sr.WriteLine($" Public DNS / [IP]: {i.PrivateDnsName} / [{i.PrivateIpAddress}]");
                            sr.WriteLine($"Private DNS / [IP]: {i.PublicDnsName} / [{i.PublicIpAddress}]");
                            sr.WriteLine($"              Tags:");
                            foreach (var t in i.Tags)
                            {
                                sr.WriteLine($"             {t.Key} : {t.Value}");
                                sr.WriteLine($"                ===========================================");
                            }

                            //if (i.State?.Name == InstanceStateName.Stopped)
                            //{
                            //    ec2.StartInstancesAsync(new StartInstancesRequest(new List<string> { i.InstanceId }));
                            //    var currState = string.Empty;
                            //    SpinWait.SpinUntil(() => {
                            //        Thread.Sleep(5 * 1000);
                            //        var dscInstRslt = ec2.DescribeInstancesAsync().Result;
                            //        var instCk = dscInstRslt.Reservations.FirstOrDefault(o => o.Instances.Any(o2 => o2.InstanceId == i.InstanceId))?.Instances.FirstOrDefault(o => o.InstanceId == i.InstanceId);
                            //        currState = instCk?.State?.Name;
                            //        //sr.WriteLine($"...start {i.InstanceId} - {currState}");
                            //        return currState != InstanceStateName.Stopped;
                            //    }, 60 * 1000);
                            //}

                            if (i.State?.Name == InstanceStateName.Running)
                            {
                                ec2.StopInstancesAsync(new StopInstancesRequest(new List<string> {i.InstanceId}));
                                var currState = string.Empty;
                                SpinWait.SpinUntil(() => {
                                    Thread.Sleep(5 * 1000);
                                    var dscInstRslt = ec2.DescribeInstancesAsync().Result;
                                    var instCk = dscInstRslt.Reservations.FirstOrDefault(o => o.Instances.Any(o2 => o2.InstanceId == i.InstanceId))?.Instances.FirstOrDefault(o => o.InstanceId == i.InstanceId);
                                    currState = instCk?.State?.Name;
                                    //sr.WriteLine($"...stop {i.InstanceId} - {currState}");
                                    return currState != InstanceStateName.Running;
                                }, 60 * 1000);
                            }
                        }
                    }
                }

                catch (AmazonEC2Exception ex)
                {
                    if (ex.ErrorCode != null && ex.ErrorCode.Equals("AuthFailure"))
                    {
                        sr.WriteLine("The account you are using is not signed up for Amazon EC2.");
                        sr.WriteLine("You can sign up for Amazon EC2 at http://aws.amazon.com/ec2");
                    }
                    else
                    {
                        sr.WriteLine("Caught Exception: " + ex.Message);
                        sr.WriteLine("Response Status Code: " + ex.StatusCode);
                        sr.WriteLine("Error Code: " + ex.ErrorCode);
                        sr.WriteLine("Error Type: " + ex.ErrorType);
                        sr.WriteLine("Request ID: " + ex.RequestId);
                    }
                }
                sr.WriteLine();

                // Print the number of Amazon SimpleDB domains.
                IAmazonSimpleDB sdb = awsOptions.CreateServiceClient<IAmazonSimpleDB>();
                ListDomainsRequest sdbRequest = new ListDomainsRequest();

                try
                {
                    ListDomainsResponse sdbResponse = sdb?.ListDomainsAsync(sdbRequest).Result;

                    sr.WriteLine($"You have {sdbResponse?.DomainNames?.Count} Amazon SimpleDB domain(s) in the [{awsRegion}] region.");
                }
                catch (AmazonSimpleDBException ex)
                {
                    if (ex.ErrorCode != null && ex.ErrorCode.Equals("AuthFailure"))
                    {
                        sr.WriteLine("The account you are using is not signed up for Amazon SimpleDB.");
                        sr.WriteLine("You can sign up for Amazon SimpleDB at http://aws.amazon.com/simpledb");
                    }
                    else
                    {
                        sr.WriteLine($"Caught Exception: {ex.Message}");
                        sr.WriteLine($"Response Status Code: {ex.StatusCode}");
                        sr.WriteLine($"Error Code: {ex.ErrorCode}");
                        sr.WriteLine($"Error Type: {ex.ErrorType}");
                        sr.WriteLine($"Request ID: {ex.RequestId}");
                    }
                }
                sr.WriteLine();

                // Print the number of Amazon S3 Buckets.
                IAmazonS3 s3Client = awsOptions.CreateServiceClient<IAmazonS3>();

                try
                {
                    ListBucketsResponse response = s3Client.ListBucketsAsync().Result;
                    int numBuckets = 0;
                    if (response.Buckets != null &&
                        response.Buckets.Count > 0)
                    {
                        numBuckets = response.Buckets.Count;
                    }
                    sr.WriteLine($"You have {numBuckets} Amazon S3 bucket(s).");
                }
                catch (AmazonS3Exception ex)
                {
                    if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId") ||
                        ex.ErrorCode.Equals("InvalidSecurity")))
                    {
                        sr.WriteLine("Please check the provided AWS Credentials.");
                        sr.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
                    }
                    else
                    {
                        sr.WriteLine($"Caught Exception: {ex.Message}");
                        sr.WriteLine($"Response Status Code: {ex.StatusCode}");
                        sr.WriteLine($"Error Code: {ex.ErrorCode}");
                        sr.WriteLine($"Request ID: {ex.RequestId}");
                    }
                }
                sr.WriteLine("Press any key to continue...");
            }
            return sb.ToString();
        }
    }
}