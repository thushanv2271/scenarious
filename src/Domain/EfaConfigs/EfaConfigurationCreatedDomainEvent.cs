using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.EfaConfigs;

/// <summary>
/// Domain event triggered when a new EFA configuration is created.
/// Contains the ID of the newly created EFA configuration.
/// </summary>
/// <param name="EfaConfigurationId">The unique identifier of the created EFA configuration.</param>
public sealed record EfaConfigurationCreatedDomainEvent(Guid EfaConfigurationId) : IDomainEvent;
