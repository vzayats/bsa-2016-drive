﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Drive.DataAccess.Entities;
using Drive.DataAccess.Interfaces;
using Drive.Identity.Entities;
using Drive.Identity.Services;
using Driver.Shared.Dto;
using Driver.Shared.Dto.Users;

namespace Drive.WebHost.Services
{
    public class FolderService : IFolderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsersService _usersService;
        private readonly IFileService _fileService;
        private readonly UsersService _userService;

        public FolderService(IUnitOfWork unitOfWork, IUsersService usersService, IFileService fileService, UsersService userService)
        {
            _unitOfWork = unitOfWork;
            _usersService = usersService;
            _fileService = fileService;
            _userService = userService;
        }

        public async Task<IEnumerable<FolderUnitDto>> GetAllAsync()
        {
            var folders = await _unitOfWork?.Folders?.Query.Select(f => new FolderUnitDto
            {
                Id = f.Id,
                Description = f.Description,
                Name = f.Name,
                IsDeleted = f.IsDeleted,
                CreatedAt = f.CreatedAt,
                LastModified = f.LastModified,
                SpaceId = f.Space.Id
            }).ToListAsync();

            return folders;
        }

        public async Task<IEnumerable<FolderUnitDto>> GetAllByParentIdAsync(int spaceId, int? parentId)
        {
            var folders = await _unitOfWork?.Folders?.Query.Where(f => f.Space.Id == spaceId)
                                                           .Where(f => f.FolderUnit.Id == parentId)
                                                           .Select(f => new FolderUnitDto
            {
                Id = f.Id,
                Description = f.Description,
                Name = f.Name,
                IsDeleted = f.IsDeleted,
                CreatedAt = f.CreatedAt,
                LastModified = f.LastModified,
                SpaceId = f.Space.Id
            }).ToListAsync();

            return folders;
        }

        public async Task<FolderUnitDto> GetAsync(int id)
        {
            var folder = await _unitOfWork?.Folders?.GetByIdAsync(id);

            if (folder == null)
                return null;

            return new FolderUnitDto
            {
                Id = folder.Id,
                Description = folder.Description,
                Name = folder.Name,
                IsDeleted = folder.IsDeleted,
                CreatedAt = folder.CreatedAt,
                LastModified = folder.LastModified,
                SpaceId = folder.Space.Id
            };
        }

        public async Task<FolderUnitDto> GetDeletedAsync(int id)
        {
            var folder = await _unitOfWork.Folders.Deleted.Where(f => f.Id == id).Select(f => new FolderUnitDto()
            {
                Id = f.Id,
                Description = f.Description,
                Name = f.Name,
                IsDeleted = f.IsDeleted,
                CreatedAt = f.CreatedAt,
                LastModified = f.LastModified,
                SpaceId = f.Space.Id
            }).SingleOrDefaultAsync();

            return folder;
        }

        public async Task<FolderUnitDto> CreateAsync(FolderUnitDto dto)
        {
            var user = await _usersService?.GetCurrentUser();
            var localUser = await _unitOfWork?.Users?.Query.FirstOrDefaultAsync(x => x.GlobalId == user.id);

            var space = await _unitOfWork?.Spaces?.GetByIdAsync(dto.SpaceId);
            var parentFolder = await _unitOfWork?.Folders?.GetByIdAsync(dto.ParentId);

            List<User> ReadPermittedUsers = new List<User>();

            ReadPermittedUsers.Add(localUser);

            List<User> ModifyPermittedUsers = new List<User>();
           
            ModifyPermittedUsers.Add(localUser);


            if (space != null)
            {
                var folder = new FolderUnit
                {
                    Description = dto.Description,
                    Name = dto.Name,

                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now,
                    IsDeleted = false,
                    Space = space,
                    FolderUnit = parentFolder,
                    Owner = await _unitOfWork.Users.Query.FirstOrDefaultAsync(u => u.GlobalId == user.id),
                    ModifyPermittedUsers = ModifyPermittedUsers,
                    ReadPermittedUsers = ReadPermittedUsers
                };

                _unitOfWork?.Folders?.Create(folder);
                await _unitOfWork?.SaveChangesAsync();

                dto.Id = folder.Id;
                dto.CreatedAt = folder.CreatedAt;
                dto.LastModified = folder.LastModified;
                dto.Author = new AuthorDto() { Id = folder.Owner.Id, Name = user.name + ' ' + user.surname };

                return dto;
            }
            return null;
        }

        public async Task<FolderUnitDto> UpdateAsync(int id, FolderUnitDto dto)
        {
            var folder = await _unitOfWork?.Folders?.GetByIdAsync(id);

            if (folder == null)
                return null;

            folder.Description = dto.Description;
            folder.IsDeleted = dto.IsDeleted;
            folder.Name = dto.Name;
            folder.LastModified = DateTime.Now;
            if (dto.ParentId != 0)
            {
                folder.FolderUnit = await _unitOfWork.Folders.GetByIdAsync(dto.ParentId);
            }

            await _unitOfWork?.SaveChangesAsync();

            dto.LastModified = DateTime.Now;

            return dto;
        }

        public async Task<FolderUnitDto> UpdateDeletedAsync(int id, int? oldParentId, FolderUnitDto dto)
        {
            var folder = await _unitOfWork?.Folders?.Deleted.Include(f => f.DataUnits).SingleOrDefaultAsync(f => f.Id == id);
            if (folder == null)
                return null;

            folder.IsDeleted = false;

            folder.Name = dto.Name;
            folder.Description = dto.Description;
            folder.IsDeleted = dto.IsDeleted;
            folder.LastModified = DateTime.Now;

            var space = await _unitOfWork.Spaces.GetByIdAsync(dto.SpaceId);

            if (oldParentId != null)
            {
                var oldParentFolder = await _unitOfWork.Folders.Query.Include(f => f.DataUnits).SingleOrDefaultAsync(f => f.Id == oldParentId);

                var list = new List<DataUnit>();
                foreach (var item in oldParentFolder.DataUnits)
                {
                    if (item.Id != folder.Id)
                    {
                        list.Add(item);
                    }
                }

                oldParentFolder.DataUnits = list;
            }

            var parentFolder = await _unitOfWork.Folders.GetByIdAsync(dto.ParentId);

            folder.Space = space;
            folder.FolderUnit = parentFolder ?? null;

            foreach (var item in folder.DataUnits)
            {
                item.IsDeleted = false;

                item.Space = await _unitOfWork.Spaces.GetByIdAsync(folder.Space.Id);

                if (item is FolderUnit)
                {
                    await ChangeSpaceId(item.Id, folder.Space.Id);
                }
            }

            await _unitOfWork?.SaveChangesAsync();

            return dto;
        }

        public async Task CreateCopyAsync(int id, FolderUnitDto dto)
        {
 
            var space = await _unitOfWork.Spaces.GetByIdAsync(dto.SpaceId);

            var user = _usersService.CurrentUserId;
            var owner = await _unitOfWork.Users.Query.FirstOrDefaultAsync(u => u.GlobalId == user);

            var folder = await _unitOfWork.Folders.GetByIdAsync(id);

            string name = folder.Name;

            if (await _unitOfWork.Folders.Query.FirstOrDefaultAsync(f => f.Name == folder.Name &&
                                        (f.FolderUnit.Id == dto.ParentId || (dto.ParentId == 0 && f.Space.Id == dto.SpaceId))) != null)
            {
                name = name + "-copy";
            }

            var destinationFolder = await _unitOfWork.Folders.GetByIdAsync(dto.ParentId);

            await CopyFolder(id, name, owner, space, destinationFolder);

        }

        private async Task CopyFolder(int folderToCopyId,string folderName,  User owner, Space space, FolderUnit destination)
        {
            var folder = await _unitOfWork?.Folders?.Query
                                                .Include(f => f.DataUnits.Select(u => u.ReadPermittedUsers))
                                                .Include(f => f.DataUnits.Select(u => u.ModifyPermittedUsers))
                                                .Include(f => f.DataUnits.Select(u => u.ReadPermittedRoles))
                                                .Include(f => f.DataUnits.Select(u => u.MorifyPermittedRoles))
                                                .Include(f => f.ModifyPermittedUsers)
                                                .Include(f => f.ReadPermittedUsers)
                                                .Include(f => f.MorifyPermittedRoles)
                                                .Include(f => f.ReadPermittedRoles)
                                                .SingleOrDefaultAsync(f => f.Id == folderToCopyId);

            if (folder == null)
                return;

            var copy = new FolderUnit
            {
                Name = folderName,
                Description = folder.Description,
                IsDeleted = folder.IsDeleted,
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                Space = space,
                Owner = owner,
                ModifyPermittedUsers = folder.ReadPermittedUsers,
                ReadPermittedUsers = folder.ModifyPermittedUsers,
                MorifyPermittedRoles = folder.MorifyPermittedRoles,
                ReadPermittedRoles = folder.ReadPermittedRoles,
                FolderUnit = destination
            };

            _unitOfWork.Folders.Create(copy);

            foreach (var subfolder in folder.DataUnits.OfType<FolderUnit>())
            {
                await CopyFolder(subfolder.Id, subfolder.Name, owner, space, copy);
            }
            foreach (var file in folder.DataUnits.OfType<FileUnit>().Where(x => !x.IsDeleted))
            {
                var newFile = new FileUnit
                {
                    Name = file.Name,
                    Description = file.Description,
                    IsDeleted = file.IsDeleted,
                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now,
                    FileType = file.FileType,
                    Link = file.Link,
                    Owner = owner,
                    Space = space,
                    FolderUnit = copy,
                    ModifyPermittedUsers = file.ReadPermittedUsers,
                    ReadPermittedUsers = file.ModifyPermittedUsers,
                    MorifyPermittedRoles = file.MorifyPermittedRoles,
                    ReadPermittedRoles = file.ReadPermittedRoles
                };

                _unitOfWork.Files.Create(newFile);
            }

            await _unitOfWork?.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var rootFolder = await _unitOfWork.Folders.Query.Include(f => f.DataUnits).SingleOrDefaultAsync(f => f.Id == id);

            rootFolder.IsDeleted = true;

            var items = rootFolder.DataUnits.Where(d => !d.IsDeleted);

            foreach (var item in items)
            {
                if (item is FolderUnit)
                {
                    var folder = await _unitOfWork.Folders.GetByIdAsync(item.Id);

                    folder.IsDeleted = true;

                    await DeleteAsync(folder.Id);
                }
                else
                {
                    var file = await _unitOfWork.Files.GetByIdAsync(item.Id);

                    file.IsDeleted = true;
                }
            }

            await _unitOfWork?.SaveChangesAsync();
        }

        public async Task<FolderContentDto> GetContentAsync(int id, int page, int count, string sort)
        {
            string userId = _userService.CurrentUserId;
            var canModifySpace = await (from s in _unitOfWork.Spaces.Query
                                    let userCanRead = s.ReadPermittedUsers.Any(x => x.GlobalId == userId)
                                    let roleCanRead = s.ReadPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                    let userCanModify = s.ModifyPermittedUsers.Any(x => x.GlobalId == userId)
                                    let roleCanModify = s.ModifyPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                    where s.ContentList.Any(f => f.Id == id) 
                                     && (s.Type == SpaceType.BinarySpace
                                     || s.Owner.GlobalId == userId
                                     || userCanRead || roleCanRead
                                     || userCanModify || roleCanModify)
                                    select s.Type == SpaceType.BinarySpace ?
                                            true : s.Owner.GlobalId == userId ?
                                                true : userCanModify ?
                                                    true : roleCanModify ?
                                                        true : false).SingleAsync();

            IEnumerable<FolderUnitDto> folders = await (from f in _unitOfWork.Folders.Query
                                                        let userCanRead = f.Space.ReadPermittedUsers.Any(x => x.GlobalId == userId)
                                                        let roleCanRead = f.Space.ReadPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                                        let userCanModify = f.Space.ModifyPermittedUsers.Any(x => x.GlobalId == userId)
                                                        let roleCanModify = f.Space.ModifyPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                                        where f.FolderUnit.Id == id
                                                             && (f.Space.Type == SpaceType.BinarySpace
                                                             || f.Space.Owner.GlobalId == userId
                                                             || userCanRead || roleCanRead
                                                             || userCanModify || roleCanModify)
                                                        select new FolderUnitDto
                                                        {
                                                            Id = f.Id,
                                                            Name = f.Name,
                                                            Description = f.Description,
                                                            CreatedAt = f.CreatedAt,
                                                            IsDeleted = f.IsDeleted,
                                                            SpaceId = f.Space.Id,
                                                            Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId },
                                                            CanRead = f.Space.Type == SpaceType.BinarySpace ?
                                                            true : f.Space.Owner.GlobalId == userId ?
                                                                true : f.Owner.GlobalId == userId ?
                                                                   true : userCanRead ?
                                                                       true : roleCanRead ?
                                                                           true : false,
                                                            CanModify = f.Space.Type == SpaceType.BinarySpace ?
                                                            true : f.Space.Owner.GlobalId == userId ?
                                                               true : f.Owner.GlobalId == userId ?
                                                                    true : userCanModify ?
                                                                       true : roleCanModify ?
                                                                           true : false
                                                        }).ToListAsync();
            IEnumerable<FileUnitDto> files = await (from f in _unitOfWork.Files.Query
                                                    let userCanRead = f.Space.ReadPermittedUsers.Any(x => x.GlobalId == userId)
                                                    let roleCanRead = f.Space.ReadPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                                    let userCanModify = f.Space.ModifyPermittedUsers.Any(x => x.GlobalId == userId)
                                                    let roleCanModify = f.Space.ModifyPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                                    where f.FolderUnit.Id == id
                                                         && (f.Space.Type == SpaceType.BinarySpace
                                                         || f.Space.Owner.GlobalId == userId
                                                         || userCanRead || roleCanRead
                                                         || userCanModify || roleCanModify)
                                                    select new FileUnitDto
                                                    {
                                                        Description = f.Description,
                                                        FileType = f.FileType,
                                                        Id = f.Id,
                                                        IsDeleted = f.IsDeleted,
                                                        Name = f.Name,
                                                        CreatedAt = f.CreatedAt,
                                                        Link = f.Link,
                                                        Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId },
                                                        CanRead = f.Space.Type == SpaceType.BinarySpace ?
                                                        true : f.Space.Owner.GlobalId == userId ?
                                                            true : f.Owner.GlobalId == userId ?
                                                               true : userCanRead ?
                                                                   true : roleCanRead ?
                                                                       true : false,
                                                        CanModify = f.Space.Type == SpaceType.BinarySpace ?
                                                        true : f.Space.Owner.GlobalId == userId ?
                                                           true : f.Owner.GlobalId == userId ?
                                                                true : userCanModify ?
                                                                   true : roleCanModify ?
                                                                       true : false
                                                    }).ToListAsync();

            if (sort != null && sort.Equals("asc"))
            {
                var foldersOrdered = folders.OrderBy(f => f.CreatedAt);
                var filesOrdered = files.OrderBy(f => f.CreatedAt);

                folders = foldersOrdered;
                files = filesOrdered;
            }
            else if (sort != null && sort.Equals("desc"))
            {
                var foldersOrdered = folders.OrderByDescending(f => f.CreatedAt);
                var filesOrdered = files.OrderByDescending(f => f.CreatedAt);

                folders = foldersOrdered;
                files = filesOrdered;
            }

            int skipCount = (page - 1) * count;
            if (folders.Count() <= skipCount)
            {
                skipCount -= folders.Count();
                folders = new List<FolderUnitDto>();
                files = files.Skip(skipCount).Take(count);
            }
            else
            {
                folders = folders.Skip(skipCount).Take(count);
                count -= folders.Count();
                files = files.Take(count);
            }

            var owners = (await _usersService.GetAllAsync()).Select(f => new { Id = f.id, Name = f.name });

            Parallel.ForEach(files, file =>
            {
                file.Author.Name = owners.FirstOrDefault(o => o.Id == file.Author.GlobalId)?.Name;
            });
            Parallel.ForEach(folders, folder =>
            {
                folder.Author.Name = owners.FirstOrDefault(o => o.Id == folder.Author.GlobalId)?.Name;
            });


            return new FolderContentDto
            {
                Files = files,
                Folders = folders,
                CanModifySpace = canModifySpace
            };
        }
        public async Task<FolderContentDto> GetContentAsync(int id)
        {
            IEnumerable<FolderUnitDto> folders = await _unitOfWork.Folders.Query.Where(x => x.FolderUnit.Id == id)
                .Select(f => new FolderUnitDto
                {
                    Id = f.Id,
                }).ToListAsync();
            IEnumerable<FileUnitDto> files = await _unitOfWork.Files.Query.Where(x => x.FolderUnit.Id == id)
                .Select(f => new FileUnitDto
                {
                    Id = f.Id,
                }).ToListAsync();

            return new FolderContentDto
            {
                Files = files,
                Folders = folders
            };
        }


        public async Task<int> GetContentTotalAsync(int id)
        {
            int counter = 0;
            string userId = _userService.CurrentUserId;

            var folders = await (from f in _unitOfWork.Folders.Query
                                                        let userCanRead = f.Space.ReadPermittedUsers.Any(x => x.GlobalId == userId)
                                                        let roleCanRead = f.Space.ReadPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                                        let userCanModify = f.Space.ModifyPermittedUsers.Any(x => x.GlobalId == userId)
                                                        let roleCanModify = f.Space.ModifyPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                                        where f.FolderUnit.Id == id
                                                             && (f.Space.Type == SpaceType.BinarySpace
                                                             || f.Space.Owner.GlobalId == userId
                                                             || userCanRead || roleCanRead
                                                             || userCanModify || roleCanModify)
                                                        select f).CountAsync();

            var files = await (from f in _unitOfWork.Files.Query
                                                    let userCanRead = f.Space.ReadPermittedUsers.Any(x => x.GlobalId == userId)
                                                    let roleCanRead = f.Space.ReadPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                                    let userCanModify = f.Space.ModifyPermittedUsers.Any(x => x.GlobalId == userId)
                                                    let roleCanModify = f.Space.ModifyPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                                    where f.FolderUnit.Id == id
                                                         && (f.Space.Type == SpaceType.BinarySpace
                                                         || f.Space.Owner.GlobalId == userId
                                                         || userCanRead || roleCanRead
                                                         || userCanModify || roleCanModify)
                                                    select f).CountAsync();

            counter += folders;
            counter += files;
            return counter;
        }

        public void Dispose()
        {
            _unitOfWork?.Dispose();
        }

        private async Task ChangeSpaceId(int id, int spaceId)
        {
            var folder =
                await _unitOfWork?.Folders?.Deleted.Include(f => f.DataUnits).SingleOrDefaultAsync(f => f.Id == id);

            foreach (var item in folder.DataUnits)
            {
                item.IsDeleted = false;

                item.Space = await _unitOfWork.Spaces.GetByIdAsync(spaceId);

                if (item is FolderUnit)
                {
                    await ChangeSpaceId(item.Id, spaceId);
                }
            }
        }
    }
}