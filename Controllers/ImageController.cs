using System.Text;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Google.Protobuf;
using Grpc.Core;
using ImageCompress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using static ImageCompress.ImageService;
using System;

namespace ImageCompress.Controllers;
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly ILogger<ImageController> _logger;
    private readonly ImageServiceClient _imageServiceClient;

    public ImageController(ILogger<ImageController> logger,
    ImageServiceClient imageServiceClient)
    {
        _logger = logger;
        _imageServiceClient = imageServiceClient;
    }
    [HttpPost]
    [RequestSizeLimit(bytes: 60_000_000)]
    public async Task<ICollection<ImageInfoItem>> UploadImage([FromForm] FileUpdateRequest form)
    {
        try
        {
            var imageIntoItemList = new List<ImageInfoItem>();
            var files = form.File;
            if (files == null)
                return imageIntoItemList;
            var accountId = User.FindFirstValue("accountId");
            var requestStream = _imageServiceClient.UploadImage();

            foreach (var file in files)
            {
                var buffer = new byte[1024];
                int bytesRead;

                var stream = new MemoryStream();
                file.CopyTo(stream);
                stream.Position = 0;
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await requestStream.RequestStream.WriteAsync(new UploadRequest
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        Quality = form.Quality,
                        AccountId = accountId,
                        FileContent = ByteString.CopyFrom(buffer, 0, bytesRead)
                    });
                }

                await requestStream.RequestStream.CompleteAsync();
                var response = await requestStream.ResponseAsync;
                imageIntoItemList.Add(response.Image);
            }
            return imageIntoItemList;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    [HttpGet]
    public async Task<ICollection<ImageInfoItem>> Get()
    {
        try
        {
            var accountId = User.FindFirstValue("accountId");
            var response = await _imageServiceClient.GetImageInfoAsync(new GetImageInfoRequest { AccountId = accountId });
            return response.Images;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    [AllowAnonymous]
    [HttpGet("ResizeImage")]
    public async Task<IActionResult> GetResizeImage(int width, string fileId)
    {
        try
        {
            using var response = _imageServiceClient.DownloadImage(new DownloadRequest { FileId = fileId });
            using var oStream = new MemoryStream();
            var contentType = "";
            while (await response.ResponseStream.MoveNext())
            {
                if (!string.IsNullOrEmpty(response.ResponseStream.Current.ContentType))
                { contentType = response.ResponseStream.Current.ContentType; }
                await oStream.WriteAsync(response.ResponseStream.Current.FileContent.ToByteArray());
            }
            oStream.Position = 0;
            using var imgBmp = SKBitmap.Decode(oStream);
            var oWidth = imgBmp.Width;
            var oHeight = imgBmp.Height;
            var height = oHeight;
            if (width > oWidth)
            {
                width = oWidth;
            }
            else
            {
                height = width * oHeight / oWidth;
            }
            using var resizedBitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(resizedBitmap);
            using var surface = SKSurface.Create(new SKImageInfo
            {
                Width = width,
                Height = height,
                ColorType = SKImageInfo.PlatformColorType,
                AlphaType = SKAlphaType.Premul
            });
            // 使用高品質的縮放模式
            var paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High
            };

            // 繪製調整後的圖片
            surface.Canvas.DrawBitmap(imgBmp, SKRect.Create(resizedBitmap.Width, resizedBitmap.Height), paint);
            surface.Canvas.Flush();
            // 將調整後的圖片轉換成位元組陣列
            using var newImage = surface.Snapshot();
            using var data = newImage.Encode(SKEncodedImageFormat.Png, 100); // 選擇適合的圖片格式和壓縮品質
            return new FileContentResult(data.ToArray(), contentType);
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    [HttpDelete]
    public async Task<ActionResult> Delete(ImageInfoItem imageInfoItem)
    {
        try
        {
            var response = await _imageServiceClient.DeleteImageAsync(new DeleteRequest { Image = imageInfoItem });
            if (response.Success)
                return Ok();
            else
                return BadRequest(response.Message);
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    [AllowAnonymous]
    [HttpGet("Download")]
    public async Task<FileContentResult> Download(string fileId, bool isCompressed)
    {
        try
        {
            using var response = _imageServiceClient.DownloadImage(new DownloadRequest { FileId = fileId, IsCompressed = isCompressed });
            using var ms = new MemoryStream();
            var contentType = "";
            var fileName = "";
            await foreach (var chunk in response.ResponseStream.ReadAllAsync())
            {
                if (!string.IsNullOrEmpty(chunk.ContentType))
                { contentType = chunk.ContentType; }
                if (!string.IsNullOrEmpty(chunk.FileName))
                { fileName = chunk.FileName; }
                await ms.WriteAsync(chunk.FileContent.ToByteArray());
            }
            // var response = await _imageServiceClient.DownloadImageAsync(new DownloadRequest { FileId = fileId });
            return File(ms.ToArray(), contentType, fileName);
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}

public class FileUpdateRequest
{
    public int Quality { get; set; }
    public IFormFile[]? File { get; set; }

}
