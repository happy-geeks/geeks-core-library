﻿using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Core.Controllers;

[Area("Cache")]
public class CacheController(ICacheService cacheService) : Controller
{
    [Route("clear{cacheArea}cache.gcl")]
    [HttpGet]
    public IActionResult ClearCacheInArea(CacheAreas cacheArea)
    {
        if (cacheArea == CacheAreas.Unknown)
        {
            // Treat unknown cache areas as 404.
            return NotFound();
        }

        cacheService.ClearCacheInArea(cacheArea);
        return Ok();
    }

    [Route("clearmemorycache.gcl")]
    [Route("clearcache.gcl")]
    [Route("clearcache.jcl")]
    [HttpGet]
    public IActionResult ClearCache()
    {
        cacheService.ClearMemoryCache();
        return Ok();
    }

    [Route("clearcontentcache.gcl")]
    [Route("clearcontentcache.jcl")]
    [HttpGet]
    public IActionResult ClearOutputCache()
    {
        cacheService.ClearOutputCache();
        return Ok();
    }

    [Route("clearfilescache.gcl")]
    [Route("clearfilescache.jcl")]
    [HttpGet]
    public IActionResult ClearImageCache()
    {
        cacheService.ClearFilesCache();
        return Ok();
    }

    [Route("clearallcache.gcl")]
    [Route("clearallcache.jcl")]
    [HttpGet]
    public IActionResult ClearAllCache()
    {
        cacheService.ClearAllCache();
        return Ok();
    }
}