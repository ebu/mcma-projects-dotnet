#load "../../build/task.csx"
#load "../../build/build.csx"

#r "nuget:Newtonsoft.Json, 12.0.2"
#r "nuget:AWSSDK.CognitoIdentityProvider, 3.3.102.40"
#r "nuget:AWSSDK.Extensions.CognitoAuthentication, 0.9.4"
#r "nuget:AWSSDK.S3, 3.3.104.5"
#r "nuget:Mcma.Core, 0.5.3.2"
#r "nuget:Mcma.Client, 0.5.3.2"
#r "nuget:Mcma.Aws.Client, 0.5.3.2"

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json.Linq;

using Mcma.Core;
using Mcma.Client;
using Mcma.Aws.Client;

public class UpdateServiceRegistry : BuildTask
{
    private static readonly JObject AwsCredentialsJson = JObject.Parse(File.ReadAllText("./deployment/aws-credentials.json"));
    private static readonly AWSCredentials AwsCredentials = new BasicAWSCredentials(AwsCredentialsJson["accessKeyId"].Value<string>(), AwsCredentialsJson["secretAccessKey"].Value<string>());
    private static readonly RegionEndpoint AwsRegion = RegionEndpoint.GetBySystemName(AwsCredentialsJson["region"].Value<string>());

    private static AwsV4AuthContext ServicesAuthContext { get; } =
        new AwsV4AuthContext(
            AwsCredentialsJson["accessKeyId"].Value<string>(),
            AwsCredentialsJson["secretAccessKey"].Value<string>(),
            AwsCredentialsJson["region"].Value<string>()
        );

    private static IResourceManagerProvider ResourceManagerProvider { get; } =
        new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(ServicesAuthContext));

    private static readonly IDictionary<string, JobProfile> JOB_PROFILES = new Dictionary<string, JobProfile>
    {
        ["ConformWorkflow"] = new JobProfile
        {
            Name = "ConformWorkflow",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "metadata", ParameterType = nameof(DescriptiveMetadata)},
                new JobParameter {ParameterName = "inputFile", ParameterType = nameof(Locator)}
            },
            OutputParameters = new[]
            {
                new JobParameter {ParameterName = "websiteMediaFile", ParameterType = nameof(Locator)},
                new JobParameter {ParameterName = "aiWorkflow", ParameterType = nameof(WorkflowJob)},
                new JobParameter {ParameterName = "bmContent", ParameterType = nameof(BMContent)}
            }
        },
        ["AiWorkflow"] = new JobProfile
        {
            Name = "AiWorkflow",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "bmContent", ParameterType = nameof(BMContent)},
                new JobParameter {ParameterName = "bmEssence", ParameterType = nameof(BMEssence)}
            }
        },
        ["ExtractTechnicalMetadata"] = new JobProfile
        {
            Name = "ExtractTechnicalMetadata",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "inputFile", ParameterType = nameof(Locator)},
                new JobParameter {ParameterName = "outputLocation", ParameterType = nameof(Locator)}
            },
            OutputParameters = new[]
            {
                new JobParameter {ParameterName = "outputFile", ParameterType = nameof(Locator)}
            }
        },
        ["CreateProxyLambda"] = new JobProfile
        {
            Name = "CreateProxyLambda",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "inputFile", ParameterType = nameof(Locator)},
                new JobParameter {ParameterName = "outputLocation", ParameterType = nameof(Locator)}
            },
            OutputParameters = new[]
            {
                new JobParameter {ParameterName = "outputFile", ParameterType = nameof(Locator)}
            }
        },
        ["CreateProxyEC2"] = new JobProfile
        {
            Name = "CreateProxyEC2",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "inputFile", ParameterType = nameof(Locator)},
                new JobParameter {ParameterName = "outputLocation", ParameterType = nameof(Locator)}
            },
            OutputParameters = new[]
            {
                new JobParameter {ParameterName = "outputFile", ParameterType = nameof(Locator)}
            }
        },
        ["ExtractThumbnail"] = new JobProfile
        {
            Name = "ExtractThumbnail",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "inputFile", ParameterType = nameof(Locator)},
                new JobParameter {ParameterName = "outputLocation", ParameterType = nameof(Locator)}
            },
            OutputParameters = new[]
            {
                new JobParameter {ParameterName = "outputFile", ParameterType = nameof(Locator)}
            },
            OptionalInputParameters = new[]
            {
                new JobParameter {ParameterName = "ebucore:width"},
                new JobParameter {ParameterName = "ebucore:height"}
            }
        },
        ["AWSTranscribeAudio"] = new JobProfile
        {
            Name = "AWSTranscribeAudio",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "inputFile", ParameterType = nameof(Locator)},
                new JobParameter {ParameterName = "outputLocation", ParameterType = nameof(Locator)}
            },
            OutputParameters = new[]
            {
                new JobParameter {ParameterName = "outputFile", ParameterType = nameof(Locator)}
            }
        },
        ["AWSTranslateText"] = new JobProfile
        {
            Name = "AWSTranslateText",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "inputFile", ParameterType = nameof(Locator)},
                new JobParameter {ParameterName = "targetLanguageCode", ParameterType = "awsLanguageCode"},
                new JobParameter {ParameterName = "outputLocation", ParameterType = nameof(Locator)}
            },
            OutputParameters = new[]
            {
                new JobParameter {ParameterName = "outputFile", ParameterType = nameof(Locator)}
            },
            OptionalInputParameters = new[]
            {
                new JobParameter {ParameterName = "sourceLanguageCode", ParameterType = "awsLanguageCode"}
            }
        },
        ["AWSDetectCelebrities"] = new JobProfile
        {
            Name = "AWSDetectCelebrities",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "inputFile", ParameterType = nameof(Locator)},
                new JobParameter {ParameterName = "outputLocation", ParameterType = nameof(Locator)}
            },
            OutputParameters = new[]
            {
                new JobParameter {ParameterName = "outputFile", ParameterType = nameof(Locator)}
            }
        },
        ["AzureExtractAllAIMetadata"] = new JobProfile
        {
            Name = "AzureExtractAllAIMetadata",
            InputParameters = new[]
            {
                new JobParameter {ParameterName = "inputFile", ParameterType = nameof(Locator)},
                new JobParameter {ParameterName = "outputLocation", ParameterType = nameof(Locator)}
            },
            OutputParameters = new[]
            {
                new JobParameter {ParameterName = "outputFile", ParameterType = nameof(Locator)}
            }
        }
    };

    private async Task ConfigureCognito(IDictionary<string, string> terraformOutput, string servicesUrl)
    {
        // 1. (Re)create cognito user for website
        const string username = "mcma";
        const string tempPassword = "b9BC9aX6B3yQK#nr";
        const string password = "%bshgkUTv*RD$sR7";

        var cognito = new AmazonCognitoIdentityProviderClient(AwsCredentials, AwsRegion);

        try
        {
            var deleteParams = new AdminDeleteUserRequest
            {
                UserPoolId = terraformOutput["cognito_user_pool_id"],
                Username = username
            };

            Console.WriteLine("Deleting existing user");
            await cognito.AdminDeleteUserAsync(deleteParams);
        }
        catch (Exception error)
        {
            Console.WriteLine($"Failed to delete existing user: {error}");
        }

        try
        {
            var createParams = new AdminCreateUserRequest
            {
                UserPoolId = terraformOutput["cognito_user_pool_id"],
                Username = username,
                MessageAction = "SUPPRESS",
                TemporaryPassword = tempPassword
            };

            Console.WriteLine("Creating user '" + username + "' with temporary password");
            var data = await cognito.AdminCreateUserAsync(createParams);
            
            var userPool = new CognitoUserPool(
                terraformOutput["cognito_user_pool_id"],
                terraformOutput["cognito_user_pool_client_id"],
                cognito);

            var user = new CognitoUser(
                username,
                terraformOutput["cognito_user_pool_client_id"],
                userPool,
                cognito);

            Console.WriteLine("Authenticating user '" + username + "' with temporary password");
            var authResponse =
                await user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest {Password = tempPassword});
            
            if (authResponse.ChallengeName == ChallengeNameType.NEW_PASSWORD_REQUIRED)
            {
                Console.WriteLine("Changing temporary password to final password");
                authResponse = await user.RespondToNewPasswordRequiredAsync(new RespondToNewPasswordRequiredRequest
                {
                    SessionID = authResponse.SessionID,
                    NewPassword = password
                });
            }
        }
        catch (Exception error)
        {
            Console.WriteLine($"Failed to setup user due to error: {error}");
        }

        // 2. Uploading configuration to website
        Console.WriteLine("Uploading deployment configuration to website");
        var config = JObject.FromObject(new
        {
            resourceManager = new
            {
                servicesUrl = terraformOutput["services_url"],
                servicesAuthType = terraformOutput["services_auth_type"]
            },
            aws = new
            {
                region = terraformOutput["aws_region"],
                s3 = new
                {
                    uploadBucket = terraformOutput["upload_bucket"]
                },
                cognito = new
                {
                    userPool = new
                    {
                        UserPoolId = terraformOutput["cognito_user_pool_id"],
                        ClientId = terraformOutput["cognito_user_pool_client_id"]
                    },
                    identityPool = new
                    {
                        id = terraformOutput["cognito_identity_pool_id"]
                    }
                }
            }
        });

        var s3Params = new PutObjectRequest
        {
            BucketName = terraformOutput["website_bucket"],
            Key = "config.json",
            ContentBody = config.ToString(),
            ContentType = "application/json"
        };

        try
        {
            var s3 = new AmazonS3Client(AwsCredentials, AwsRegion);
            await s3.PutObjectAsync(s3Params);
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return;
        }
    }

    protected override async Task<bool> ExecuteTask()
    {
        var content = File.ReadAllText($"{Build.Dirs.Deployment.TrimEnd('/')}/terraform.output");
        var terraformOutput = ParseContent(content);
        
        var servicesUrl = terraformOutput["services_url"];
        var servicesAuthType = terraformOutput["services_auth_type"];

        var jobProfilesUrl = $"{terraformOutput["service_registry_url"]}/job-profiles";

        await ConfigureCognito(terraformOutput, servicesUrl);

        var serviceRegistry = new Service
        {
            Name = "Service Registry",
            Resources = new[]
            {
                new ResourceEndpoint {ResourceType = nameof(Service), HttpEndpoint = servicesUrl},
                new ResourceEndpoint {ResourceType = nameof(JobProfile), HttpEndpoint = jobProfilesUrl}
            },
            AuthType = "AWS4"
        };
        
        var resourceManager = ResourceManagerProvider.Get(servicesUrl, servicesAuthType);
        
        Console.WriteLine("Getting existing services...");
        var retrievedServices = await resourceManager.GetAsync<Service>();
        
        foreach (var retrievedService in retrievedServices)
        {
            if (retrievedService.Name == "Service Registry")
            {
                if (serviceRegistry.Id == null)
                {
                    serviceRegistry.Id = retrievedService.Id;

                    Console.WriteLine("Updating Service Registry");
                    await resourceManager.UpdateAsync(serviceRegistry);
                }
                else
                {
                    Console.WriteLine("Removing duplicate Service Registry '" + retrievedService.Id + "'");
                    await resourceManager.DeleteAsync(retrievedService);
                }
            }
        }

        if (serviceRegistry.Id == null)
        {
            Console.WriteLine("Inserting Service Registry");
            serviceRegistry = await resourceManager.CreateAsync(serviceRegistry);
        }

        await resourceManager.InitAsync();

        var retrievedJobProfiles = await resourceManager.GetAsync<JobProfile>();

        foreach (var retrievedJobProfile in retrievedJobProfiles)
        {
            if (JOB_PROFILES.ContainsKey(retrievedJobProfile.Name))
            {
                var jobProfile = JOB_PROFILES[retrievedJobProfile.Name];
                jobProfile.Id = retrievedJobProfile.Id;

                Console.WriteLine("Updating JobProfile '" + jobProfile.Name + "'");
                await resourceManager.UpdateAsync(jobProfile);
            }
            else
            {
                Console.WriteLine("Removing JobProfile '" + retrievedJobProfile.Name + "'");
                await resourceManager.DeleteAsync(retrievedJobProfile);
            }
        }

        foreach (var jobProfileName in JOB_PROFILES.Keys.ToList())
        {
            var jobProfile = JOB_PROFILES[jobProfileName];
            if (jobProfile.Id == null)
            {
                Console.WriteLine("Inserting JobProfile '" + jobProfile.Name + "'");
                JOB_PROFILES[jobProfileName] = await resourceManager.CreateAsync(jobProfile);
            }
        }

        var SERVICES = CreateServices(terraformOutput);

        retrievedServices = await resourceManager.GetAsync<Service>();

        foreach (var retrievedService in retrievedServices.ToList())
        {
            if (retrievedService.Name == serviceRegistry.Name)
                continue;

            if (SERVICES.ContainsKey(retrievedService.Name))
            {
                var service = SERVICES[retrievedService.Name];
                service.Id = retrievedService.Id;

                Console.WriteLine("Updating Service '" + service.Name + "'");
                await resourceManager.UpdateAsync(service);
            }
            else
            {
                Console.WriteLine("Removing Service '" + retrievedService.Name + "'");
                await resourceManager.DeleteAsync(retrievedService);
            }
        }

        foreach (var serviceName in SERVICES.Keys.ToList())
        {
            var service = SERVICES[serviceName];
            if (service.Id == null)
            {
                Console.WriteLine("Inserting Service '" + service.Name + "'");
                SERVICES[serviceName] = await resourceManager.CreateAsync(service);
            }
        }

        return true;
    }

    private IDictionary<string, string> ParseContent(string content)
    {
        var serviceUrls = new Dictionary<string, string>();

        foreach (var line in content.Split('\n'))
        {
            var parts = line.Split(new[] {" = "}, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
                serviceUrls[parts[0]] = parts[1].Trim();
        }

        return serviceUrls;
    }

    private static IDictionary<string, Service> CreateServices(IDictionary<string, string> serviceUrls)
    {
        var serviceList = new List<Service>();

        foreach (var prop in serviceUrls.Keys)
        {
            switch (prop)
            {
                case "ame_service_url":
                    serviceList.Add(new Service
                    {
                        Name = "MediaInfo AME Service",
                        Resources = new[]
                        {
                            new ResourceEndpoint {ResourceType = nameof(JobAssignment), HttpEndpoint = serviceUrls[prop] + "/job-assignments"}
                        },
                        JobType = nameof(AmeJob),
                        JobProfiles = new string[]
                        {
                            JOB_PROFILES["ExtractTechnicalMetadata"].Id
                        },
                        AuthType = "AWS4"
                    });
                    break;
                case "aws_ai_service_url":
                    serviceList.Add(new Service
                    {
                        Name = "AWS AI Service",
                        Resources = new[]
                        {
                            new ResourceEndpoint {ResourceType = "JobAssignment", HttpEndpoint = serviceUrls[prop] + "/job-assignments"}
                        },
                        JobType = nameof(AIJob),
                        JobProfiles = new string[]
                        {
                            JOB_PROFILES["AWSTranscribeAudio"].Id,
                            JOB_PROFILES["AWSTranslateText"].Id,
                            JOB_PROFILES["AWSDetectCelebrities"].Id
                        },
                        AuthType = "AWS4"
                    });
                    break;
                case "azure_ai_service_url":
                    serviceList.Add(new Service
                    {
                        Name = "AZURE AI Service",
                        Resources = new[]
                        {
                            new ResourceEndpoint {ResourceType = "JobAssignment", HttpEndpoint = serviceUrls[prop] + "/job-assignments"}
                        },
                        JobType = nameof(AIJob),
                        JobProfiles = new[]
                        {
                            JOB_PROFILES["AzureExtractAllAIMetadata"].Id
                        },
                        AuthType = "AWS4"
                    });
                    break;
                case "job_processor_service_url":
                    serviceList.Add(new Service
                    {
                        Name = "Job Processor Service",
                        Resources = new[]
                        {
                            new ResourceEndpoint {ResourceType = nameof(JobProcess), HttpEndpoint = serviceUrls[prop] + "/job-processes"}
                        },
                        AuthType = "AWS4"
                    });
                    break;
                case "job_repository_url":
                    serviceList.Add(new Service
                    {
                        Name = "Job Repository",
                        Resources = new[]
                        {
                            new ResourceEndpoint {ResourceType = "AmeJob", HttpEndpoint = serviceUrls[prop] + "/jobs"},
                            new ResourceEndpoint {ResourceType = "AIJob", HttpEndpoint = serviceUrls[prop] + "/jobs"},
                            new ResourceEndpoint {ResourceType = "CaptureJob", HttpEndpoint = serviceUrls[prop] + "/jobs"},
                            new ResourceEndpoint {ResourceType = "QAJob", HttpEndpoint = serviceUrls[prop] + "/jobs"},
                            new ResourceEndpoint {ResourceType = "TransferJob", HttpEndpoint = serviceUrls[prop] + "/jobs"},
                            new ResourceEndpoint {ResourceType = "TransformJob", HttpEndpoint = serviceUrls[prop] + "/jobs"},
                            new ResourceEndpoint {ResourceType = nameof(WorkflowJob), HttpEndpoint = serviceUrls[prop] + "/jobs"}
                        },
                        AuthType = "AWS4"
                    });
                    break;
                case "media_repository_url":
                    serviceList.Add(new Service
                    {
                        Name = "Media Repository",
                        Resources = new[]
                        {
                            new ResourceEndpoint {ResourceType = "BMContent", HttpEndpoint = serviceUrls[prop] + "/bm-contents"},
                            new ResourceEndpoint {ResourceType = "BMEssence", HttpEndpoint = serviceUrls[prop] + "/bm-essences"}
                        },
                        AuthType = "AWS4"
                    });
                    break;
                case "transform_service_url":
                    serviceList.Add(new Service
                    {
                        Name = "FFmpeg TransformService",
                        Resources = new[]
                        {
                            new ResourceEndpoint {ResourceType = "JobAssignment", HttpEndpoint = serviceUrls[prop] + "/job-assignments"}
                        },
                        JobType = nameof(TransformJob),
                        JobProfiles = new string[]
                        {
                            JOB_PROFILES["CreateProxyLambda"].Id,
                            JOB_PROFILES["CreateProxyEC2"].Id
                        },
                        AuthType = "AWS4"
                    });
                    break;
                case "workflow_service_url":
                    serviceList.Add(new Service
                    {
                        Name = "Workflow Service",
                        Resources = new[]
                        {
                            new ResourceEndpoint {ResourceType = nameof(JobAssignment), HttpEndpoint = serviceUrls[prop] + "/job-assignments"}
                        },
                        JobType = nameof(WorkflowJob),
                        JobProfiles = new string[]
                        {
                            JOB_PROFILES["ConformWorkflow"].Id,
                            JOB_PROFILES["AiWorkflow"].Id
                        },
                        AuthType = "AWS4"
                    });
                    break;
            }
        }

        return serviceList.ToDictionary(service => service.Name, service => service);
    }
}