using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Products.Commands;

public record DeleteProductImageCommand(Guid ProductId) : IRequest<bool>;

public class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductImageCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(product.ImagePath))
        {
            return true;
        }

        // Robust wwwroot path resolution
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var wwwrootPath = Path.Combine(baseDir, "wwwroot");
        if (!Directory.Exists(wwwrootPath))
        {
            var parent = Directory.GetParent(baseDir);
            while (parent != null)
            {
                var candidate = Path.Combine(parent.FullName, "wwwroot");
                if (Directory.Exists(candidate))
                {
                    wwwrootPath = candidate;
                    break;
                }
                parent = parent.Parent;
            }
        }

        // Physical path
        var physicalPath = Path.Combine(wwwrootPath, product.ImagePath.TrimStart('/'));
        if (File.Exists(physicalPath))
        {
            try
            {
                File.Delete(physicalPath);
            }
            catch
            {
                // Ignore errors
            }
        }

        // Clear db
        product.ImagePath = null;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
