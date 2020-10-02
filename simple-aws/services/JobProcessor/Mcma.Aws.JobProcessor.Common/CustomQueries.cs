using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Mcma.Data.DocumentDatabase.Queries;

namespace Mcma.Aws.JobProcessor.Common
{
    public static class CustomQueries
    {
        public static QueryOperationConfig CreateJobResourceQuery(CustomQuery<JobResourceQueryParameters> customQuery)
        {
            var (partitionKey, status, from, to, ascending, limit) = customQuery.Parameters;
            ascending = ascending ?? true;
            
            var index = status.HasValue ? "ResourceStatusIndex" : "ResourceCreatedIndex";
            var partitionKeyField = status.HasValue ? "resource_status" : "resource_pkey";
            var partitionKeyValue = status.HasValue ? $"{partitionKey}-{status}" : partitionKey;
            var keyConditionExpression = $"{partitionKeyField} = :pkey";
            var expressionAttributeValues = new Dictionary<string, DynamoDBEntry>
            {
                [":pkey"] = partitionKeyValue
            };

            if (from.HasValue && to.HasValue)
            {
                keyConditionExpression += " and resource_created BETWEEN :from AND :to";
                expressionAttributeValues[":from"] = from.Value.ToUnixTimeMilliseconds();
                expressionAttributeValues[":to"] = to.Value.ToUnixTimeMilliseconds();
            }
            else if (from.HasValue)
            {
                keyConditionExpression += " and resource_created >= :from";
                expressionAttributeValues[":from"] = from.Value.ToUnixTimeMilliseconds();
            }
            else if (to.HasValue)
            {
                keyConditionExpression += " and resource_created <= :to";
                expressionAttributeValues[":to"] = to.Value.ToUnixTimeMilliseconds();
            }
            
            return new QueryOperationConfig
            {
                IndexName = index,
                KeyExpression = new Expression
                {
                    ExpressionStatement = keyConditionExpression,
                    ExpressionAttributeValues = expressionAttributeValues
                },
                BackwardSearch = !ascending.Value,
                Limit = limit ?? int.MaxValue
            };
        }
    }
}