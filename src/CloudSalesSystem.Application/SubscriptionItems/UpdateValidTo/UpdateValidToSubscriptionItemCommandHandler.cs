﻿namespace CloudSalesSystem.Application.SubscriptionItems.UpdateValidTo;

internal sealed class UpdateValidToSubscriptionItemCommandHandler : IRequestHandler<UpdateValidToSubscriptionItemCommand, UpdateValidToSubscriptionItemResponse>
{
    private readonly ICloudComputingService _cloudComputingService;
    private readonly ILicenseRepository _licenseRepository;
    private readonly ISubscriptionItemRepository _subscriptionItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateValidToSubscriptionItemCommandHandler(
        ICloudComputingService cloudComputingService,
        ILicenseRepository licenseRepository,
        ISubscriptionItemRepository subscriptionItemRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _cloudComputingService = cloudComputingService;
        _licenseRepository = licenseRepository;
        _subscriptionItemRepository = subscriptionItemRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UpdateValidToSubscriptionItemResponse> Handle(UpdateValidToSubscriptionItemCommand request, CancellationToken cancellationToken)
    {
        var subscriptionItem = await _subscriptionItemRepository.GetByIdAsync(request.SubscriptionItemId, cancellationToken);

        if (subscriptionItem is null)
        {
            throw new InvalidOperationException("Subscription item doesn't exists!");
        }

        var licensesToExtend = (await _licenseRepository.GetAssignedLicenses(
            subscriptionItem.SubscriptionId,
            request.SubscriptionItemId,
            cancellationToken))
            .Select(license => new ExtendServiceItemLicenseKeyRequest
                (
                    subscriptionItem.SubscriptionId,
                    subscriptionItem.ProductId,
                    license.Key!,
                    request.ValidToDate
                ))
            .ToList();

        if (!await _cloudComputingService.ExtendLicenseKeys(licensesToExtend, CancellationToken.None))
        {
            throw new InvalidOperationException("Extend licenses failed!");
        }

        subscriptionItem.SetValidTo(request.ValidToDate);

        var mappedResult = _mapper.Map<UpdateValidToSubscriptionItemResponse>(subscriptionItem);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return mappedResult;
    }
}