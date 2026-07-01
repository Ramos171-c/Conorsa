using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Application.Products.Commands;

public record UploadProductImageCommand(
    Guid ProductId,
    byte[] FileBytes,
    string FileName
) : IRequest<string>;

public class UploadProductImageCommandHandler : IRequestHandler<UploadProductImageCommand, string>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadProductImageCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            throw new ArgumentException("El producto especificado no existe.");
        }

        // Validate extension
        var extension = Path.GetExtension(request.FileName).ToLower();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Tipo de archivo no permitido. Solo se permiten imágenes (jpg, jpeg, png, gif, webp).");
        }

        // Create folder if it doesn't exist
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Delete old image if exists
        if (!string.IsNullOrWhiteSpace(product.ImagePath))
        {
            var oldPhysicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImagePath.TrimStart('/'));
            if (File.Exists(oldPhysicalPath))
            {
                try
                {
                    File.Delete(oldPhysicalPath);
                }
                catch
                {
                    // Ignore delete errors
                }
            }
        }

        // Generate unique filename
        var uniqueFileName = $"{product.Id}_{DateTime.UtcNow.Ticks}{extension}";
        var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

        // Write file
        await File.WriteAllBytesAsync(physicalPath, request.FileBytes, cancellationToken);

        // Save relative path
        var relativePath = $"/uploads/products/{uniqueFileName}";
        product.ImagePath = relativePath;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return relativePath;
    }
}
