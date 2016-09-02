﻿using Driver.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Drive.WebHost.Services
{
    public interface ISharedSpaceService
    {
        Task CreateOrUpdatePermissionsOfSharedDataAsync(List<UserSharedSpaceDto> users, int id);
        Task<IEnumerable<UserSharedSpaceDto>> GetPermissionsOfSharedDataAsync(int id);
        Task<SharedSpaceDto> GetAsync(int page, int count, string sort);
        Task<int> GetTotalAsync();
        Task<SharedSpaceDto> SearchAsync(string text, int page, int count);
        Task<int> SearchTotalAsync(string text);
        Task Delete(int id);

        void Dispose();
    }
}