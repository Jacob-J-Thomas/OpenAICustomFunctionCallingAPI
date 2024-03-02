﻿using OpenAICustomFunctionCallingAPI.Client;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using Newtonsoft.Json.Linq;
using OpenAICustomFunctionCallingAPI.Host.Config;
//using OpenAICustomFunctionCallingAPI.DAL;
using Azure;
using Microsoft.Data.SqlClient;
using OpenAICustomFunctionCallingAPI.DAL;
using System.Web.Mvc;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Nest;
using OpenAICustomFunctionCallingAPI.DAL.DTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using System.Reflection.Metadata;

namespace OpenAICustomFunctionCallingAPI.Business
{
    // this whole class needs some refactoring
    public class ValidationLogic
    {
        public ValidationLogic() { }

        public string ValidateChatRequest(ChatRequestDTO chatRequest)
        {
            if (chatRequest == null)
            {
                return "The chatRequest object must be provided";
            }
            if (chatRequest.Modifiers != null)
            {
                var profileDTO = new APIProfileDTO(chatRequest.Modifiers);
                var errorMessage = ValidateProfile(profileDTO);
                if (errorMessage != null)
                {
                    return errorMessage;
                }
            }

            return null;
        }

        public string ValidateProfile(APIProfileDTO profile)
        {
            var validModels = new List<string>()
            {
                "babbage-002",
                "davinci-002",
                "gpt-3.5-turbo",
                "gpt-3.5-turbo-16k",
                "gpt-3.5-turbo-instruct",
                "gpt-4",
                "gpt-4-32k",
                "gpt-4-turbo-preview",
                "gpt-4-vision-preview",
                "mixtral",
                "cusotom" // need to implement this still
            };

            // ensure tool and profiles do not have overlapping names since there
            // would be no way to tell them apart during recursive child/function calls
            //      - Actually, might be able to avoid this by appending "-tool" and
            //          "-model" when data is retrieved from the database

            // validate reference profiles exist (same with any other values?)

            if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name == null)
            {
                return "The 'Name' field is required";
            }
            if (profile.Name.ToLower() == "all")
            {
                return "Profile name 'all' conflicts with the get/all route";
            }
            if (profile.Model != null && validModels.Contains(profile.Model) == false)
            {
                return "The model name must match and existing AI model";
            }
            if (profile.Frequency_Penalty < -2.0 || profile.Frequency_Penalty > 2.0)
            {
                return "Frequency_Penalty must be a value between -2 and 2";
            }
            if (profile.Presence_Penalty < -2.0 || profile.Presence_Penalty > 2.0)
            {
                return "Presence_Penalty must be a value between -2 and 2";
            }
            if (profile.Temperature < 0 || profile.Temperature > 2)
            {
                return "Temperature must be a value between 0 and 2";
            }
            if (profile.Top_P < 0 || profile.Top_P > 1)
            {
                return "Top_P must be a value between 0 and 1";
            }
            if (profile.Max_Tokens < 1 || profile.Max_Tokens > 1000000)
            {
                return "Max_Tokens must be a value between 1 and 1,000,000";
            }
            if (profile.N < 0 || profile.N > 100)
            {
                return "N must be a value between 0 and 100";
            }
            if (profile.Top_Logprobs < 0 || profile.Top_Logprobs > 5)
            {
                return "Top_Logprobs must be a value between 0 and 5";
            }
            if (profile.Top_Logprobs != null && profile.Model == "gpt-4-vision-preview")
            {
                return "Top_Logprobs cannot be used with gpt-4-vision-preview";
            }
            if (profile.Response_Format != null && (profile.Response_Format != "text" && profile.Response_Format != "json_object"))
            {
                return "If Response_Type is set, it must either be equal to 'text' or 'json_object'";
            }

            if (profile.Tools != null)
            {
                foreach (var tool in profile.Tools)
                {
                    var errorMessage = ValidateTool(tool);
                    if (errorMessage != null)
                    {
                        return errorMessage;
                    }
                }
            }
            return null;
        }

        public string ValidateTool(Tool tool)
        {
            if (tool.Function.Name == null || string.IsNullOrEmpty(tool.Function.Name))
            {
                return "A function name is required for all tools.";
            }
            if (tool.Function.Parameters.Required != null && tool.Function.Parameters.Required.Length > 0)
            {
                foreach (var str in  tool.Function.Parameters.Required)
                {
                    if (!tool.Function.Parameters.Properties.ContainsKey(str))
                    {
                        return $"Required property {str} does not exist in the tool {tool.Function.Name}'s properties list.";
                    }
                }
            }

            if (tool.Function.Parameters.Properties != null && tool.Function.Parameters.Properties.Count > 0)
            {
                var errorMessage = ValidateProperties(tool.Function.Parameters.Properties);
                if (errorMessage != null)
                {
                    return errorMessage;
                }
            }
            return null;
        }

        public string ValidateProperties(Dictionary<string, PropertyDTO> properties)
        {
            var validTypes = new List<string>()
            {
                // verify these
                "char",
                "string",
                "bool",
                "int",
                "double",
                "float",
                "date",
                "enum",

                // Don't think these would work currently, but can test. May work as string:
                //"array",
                //"object"
            };
            foreach (var prop in properties)
            {
                if (prop.Value.Type == null)
                {
                    return $"The field 'type' for property {prop.Key} is required";
                }
                else if (!validTypes.Contains(prop.Value.Type))
                {
                    return $"The 'type' field '{prop.Value.Type}' for property {prop.Key} is invalid. Please ensure one of the following types is selected: '{validTypes}'";
                }
            }
            return null;
        }
    }
}
