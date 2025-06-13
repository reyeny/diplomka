// Utilities/Mappers/CompanyMapper.cs

using Authorization.Dto.Company;
using Authorization.Models;

namespace Authorization.Mappers
{
    public static class CompanyMapper
    {
        public static Company ToDto(this CompanyDto c) => new Company
        {
            Id = c.Id,
            Name = c.Name,
            OwnerId = c.OwnerId
        };
    }
}