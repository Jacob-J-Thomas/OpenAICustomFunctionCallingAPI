﻿using Microsoft.AspNetCore.Mvc;
using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
using OpenAICustomFunctionCallingAPI.Host.Config;
using OpenAICustomFunctionCallingAPI.Business;
using System.Runtime;
using Azure;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json.Linq;
using OpenAICustomFunctionCallingAPI.DAL;
using Nest;
using Azure.Core;
using Microsoft.AspNetCore.Routing;
using System.Reflection.Metadata;
using OpenAICustomFunctionCallingAPI.API.DTOs;
using OpenAICustomFunctionCallingAPI.Business.ProfileLogic;

namespace OpenAICustomFunctionCallingAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ToolController : ControllerBase
    {
        //private readonly IConfiguration _configuration;
        private readonly ProfileLogic _profileLogic;

        public ToolController(Settings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _profileLogic = new ProfileLogic(settings.DbConnectionString);
        }

        [HttpGet]
        [Route("get/{name}")]
        public async Task<IActionResult> GetTool([FromRoute] string name) // get this to work with either a string or an int
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                }
                var tool = await _profileLogic.GetTool(name);
                if (tool == null)
                {
                    return NotFound($"No tool with the name {name} exists");
                }
                return Ok(tool);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        [HttpGet]
        [Route("get/all")]
        public async Task<IActionResult> GetAllTools()
        {
            try
            {
                var tools = await _profileLogic.GetAllTools();
                if (tools == null || tools.Count() < 1)
                {
                    return NotFound($"No tools exist. Make a post request to add some.");
                }
                return Ok(tools);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("get/{name}/profiles")]
        public async Task<IActionResult> GetToolProfiles(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                }
                var tool = await _profileLogic.GetToolProfileAssociations(name);
                if (tool == null)
                {
                    return NotFound($"The tool '{name}' is not associated with any profiles, or does not exist.");
                }
                return Ok(tool);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("upsert")]
        public async Task<IActionResult> AddOrUpdateTool([FromBody] List<Tool> toolList)
        {
            try
            {
                var errorMessage = await _profileLogic.CreateOrUpdateTools(toolList);
                if (errorMessage != null)
                {
                    return BadRequest(errorMessage);
                }
                return NoContent();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                // Logging and transient exception handling here
                throw;
            }
        }

        [HttpPost]
        [Route("associate/{name}")]
        public async Task<IActionResult> AddToolToProfiles([FromRoute] string name, List<string> profiles)
        {
            // prevent profiles that don't exist from being added?

            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                }
                if (profiles == null || profiles.Count < 1)
                {
                    return BadRequest($"Invalid request.'Profiles' property cannot be null or empty: {profiles}.");
                }

                var errorMessage = await _profileLogic.AddToolAssociations(name, profiles);
                if (errorMessage == null)
                {
                    return Ok(await _profileLogic.GetToolProfileAssociations(name));
                }
                return NotFound(errorMessage);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("dissociate/{name}")]
        public async Task<IActionResult> RemoveToolFromProfiles([FromRoute] string name, List<string> profiles)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                }
                if (profiles == null || profiles.Count < 1)
                {
                    return BadRequest($"Invalid request.'Profiles' property cannot be null or empty: {profiles}.");
                }

                var errorMessage = await _profileLogic.DeleteToolAssociations(name, profiles);
                if (errorMessage == null)
                {
                    return NoContent();
                }
                return NotFound(errorMessage);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpDelete]
        [Route("delete/{name}")]
        public async Task<IActionResult> DeleteTool([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest($"Invalid request.Please check the route parameter for the profile name: {name}.");
                }
                var success = await _profileLogic.DeleteTool(name);
                if (success)
                {
                    return NoContent();
                }
                return NotFound($"No tool with the name {name} exists");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}