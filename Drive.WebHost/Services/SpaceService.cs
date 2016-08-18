﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Drive.DataAccess.Entities;
using Drive.DataAccess.Interfaces;
using Driver.Shared.Dto;
using Drive.Logging;
using Driver.Shared.Dto.Users;

namespace Drive.WebHost.Services
{
    public class SpaceService : ISpaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;
        private readonly IUsersService _userService;

        public SpaceService(IUnitOfWork unitOfWork, ILogger logger, IUsersService userService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userService = userService;
        }


        public async Task<SpaceDto> GetAsync(int id)
        {

           var space = await _unitOfWork.Spaces.Query.Where(s=>s.Id == id).Select(s => new SpaceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                MaxFileSize = s.MaxFileSize,
                MaxFilesQuantity = s.MaxFilesQuantity,
                ReadPermittedUsers = s.ReadPermittedUsers,
                Files = s.ContentList.OfType<FileUnit>().Where(f => f.Parent == null).Select(f => new FileUnitDto
                {
                    Description = f.Description,
                    FileType = f.FileType,
                    Id = f.Id,
                    IsDeleted = f.IsDeleted,
                    Name = f.Name,
                    Link = f.Link,
                    CreatedAt = f.CreatedAt,
                    Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId}
                }),
                Folders = s.ContentList.OfType<FolderUnit>().Where(f => f.Parent == null).Select(f => new FolderUnitDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    CreatedAt = f.CreatedAt,
                    IsDeleted = f.IsDeleted,
                    SpaceId = f.Space.Id,
                    Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId }
                })
            }).SingleOrDefaultAsync();

            var owners = (await _userService.GetAllAsync()).Select(f => new { Id = f.id, Name = f.name });

            Parallel.ForEach(space.Files, file =>
            {
                file.Author.Name = owners.FirstOrDefault(o => o.Id == file.Author.GlobalId)?.Name;
            });
            Parallel.ForEach(space.Folders, folder =>
            {
                folder.Author.Name = owners.FirstOrDefault(o => o.Id == folder.Author.GlobalId)?.Name;
            });
            return space;
        }

        public async Task<SpaceDto> GetAsync(int id, int page, int count)
        {
            var space = await _unitOfWork.Spaces.Query.Where(s => s.Id == id).Select(s => new SpaceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                MaxFileSize = s.MaxFileSize,
                MaxFilesQuantity = s.MaxFilesQuantity,
                ReadPermittedUsers = s.ReadPermittedUsers,
                Files = s.ContentList.OfType<FileUnit>().Where(f => f.Parent == null).Select(f => new FileUnitDto
                {
                    Description = f.Description,
                    FileType = f.FileType,
                    Id = f.Id,
                    IsDeleted = f.IsDeleted,
                    Name = f.Name,
                    CreatedAt = f.CreatedAt,
                    Link = f.Link,
                    Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId }
                }),
                Folders = s.ContentList.OfType<FolderUnit>().Where(f => f.Parent == null).Select(f => new FolderUnitDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    CreatedAt = f.CreatedAt,
                    IsDeleted = f.IsDeleted,
                    SpaceId = f.Space.Id,
                    Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId }
                })
            }).SingleOrDefaultAsync();

            if (space == null)
                return null;
            int skipCount = (page - 1) * count;
            if (space.Folders.Count() <= skipCount)
            {
                skipCount -= space.Folders.Count();
                space.Folders = new List<FolderUnitDto>();
                space.Files = space.Files.Skip(skipCount).Take(count);
            }
            else
            {
                space.Folders = space.Folders.Skip(skipCount).Take(count);
                count -= space.Folders.Count();
                space.Files = space.Files.Take(count);
            }

            var owners = (await _userService.GetAllAsync()).Select(f => new { Id = f.id, Name = f.name });

            Parallel.ForEach(space.Files, file =>
            {
                file.Author.Name = owners.FirstOrDefault(o => o.Id == file.Author.GlobalId)?.Name;
            });
            Parallel.ForEach(space.Folders, folder =>
            {
                folder.Author.Name = owners.FirstOrDefault(o => o.Id == folder.Author.GlobalId)?.Name;
            });


            return space;
        }

        public async Task<int> GetTotalAsync(int id)
        {
            int counter = 0;
            var space = await _unitOfWork.Spaces.Query.Where(s => s.Id == id).Select(s => new
            {
                Files = s.ContentList.OfType<FileUnit>().Where(f => f.Parent == null).Count(),
                Folders = s.ContentList.OfType<FolderUnit>().Where(f => f.Parent == null).Count()
            }).SingleOrDefaultAsync();
            if (space == null)
                return 0;
            counter += space.Files;
            counter += space.Folders;
            return counter;
        }





        public async Task<IList<SpaceDto>> GetAllAsync()
        {

            var spacesList = await _unitOfWork.Spaces.Query.Select(s => new SpaceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            }).ToListAsync();


            return spacesList;
        }

        public async Task<int> CreateAsync(SpaceDto dto)
        {
            var user = await _userService.GetCurrentUser();

            var space = new Space
            {
                Name = dto.Name,
                Description = dto.Description,
                MaxFilesQuantity = dto.MaxFilesQuantity,
                MaxFileSize = dto.MaxFileSize,
                ReadPermittedUsers = dto.ReadPermittedUsers,
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                IsDeleted = false,
                Owner = await _unitOfWork.Users.Query.FirstOrDefaultAsync(u => u.GlobalId == user.serverUserId)
            };
            _unitOfWork?.Spaces?.Create(space);
            await _unitOfWork?.SaveChangesAsync();
            return space.Id;
        }

        public async Task UpdateAsync(int id, SpaceDto dto)
        {
            var space = await _unitOfWork?.Spaces?.GetByIdAsync(id);

            if (space == null) return;

            space.Name = dto.Name;
            space.Description = dto.Description;
            space.MaxFileSize = dto.MaxFileSize;
            space.MaxFilesQuantity = dto.MaxFilesQuantity;
            space.ReadPermittedUsers = dto.ReadPermittedUsers;
            space.LastModified = DateTime.Now;

            await _unitOfWork?.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            _unitOfWork?.Spaces?.Delete(id);
            await _unitOfWork?.SaveChangesAsync();
        }


        public async Task<SearchResultDto> SearchFoldersAndFilesAsync(int spaceId, int? folderId, string text, int page, int count)
        {
            IEnumerable<FolderUnitDto> resultFolder = new List<FolderUnitDto>();
            IEnumerable<FileUnitDto> resultFiles = new List<FileUnitDto>();
            try
            {
                if (folderId != null)
                {
                    var folder = await _unitOfWork.Folders.Query.Where(f => f.Id == folderId)
                        .Select(s => new
                        {
                            Folders = s.DataUnits.OfType<FolderUnit>(),
                            Files = s.DataUnits.OfType<FileUnit>()
                        }).SingleOrDefaultAsync();
                    if (folder == null)
                        return null;
                    resultFolder = folder.Folders
                        .Where(f => f.Name.ToLower().Contains(text.ToLower()))
                        .Select(f => new FolderUnitDto()
                        {
                            Id = f.Id,
                            Name = f.Name,
                            Description = f.Description,
                            IsDeleted = f.IsDeleted,
                            CreatedAt = f.CreatedAt,
                            LastModified = f.LastModified,
                            Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId }
                        });

                    resultFiles = folder.Files
                        .Where(f => f.Name.ToLower().Contains(text.ToLower()))
                        .Select(f => new FileUnitDto
                        {
                            Id = f.Id,
                            Name = f.Name,
                            Description = f.Description,
                            FileType = f.FileType,
                            IsDeleted = f.IsDeleted,
                            Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId }
                        });
                }
                else
                {
                    var space = await _unitOfWork.Spaces.Query
                        .Where(s => s.Id == spaceId)
                        .Select(s => new
                        {
                            Folders = s.ContentList.OfType<FolderUnit>().Where(f=>f.Parent==null),
                            Files = s.ContentList.OfType<FileUnit>().Where(f => f.Parent == null)
                        }).SingleOrDefaultAsync();
                    if (space == null)
                        return null;
                    resultFolder = space.Folders
                        .Where(f => f.Name.ToLower().Contains(text.ToLower()))
                        .Select(f => new FolderUnitDto()
                        {
                            Id = f.Id,
                            Name = f.Name,
                            Description = f.Description,
                            IsDeleted = f.IsDeleted,
                            CreatedAt = f.CreatedAt,
                            Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId }
                        });
                    resultFiles = space.Files
                        .Where(f => f.Name.ToLower().Contains(text.ToLower()))
                        .Select(f => new FileUnitDto
                        {
                            Id = f.Id,
                            Name = f.Name,
                            Description = f.Description,
                            FileType = f.FileType,
                            IsDeleted = f.IsDeleted,
                            CreatedAt = f.CreatedAt,
                            Author = new AuthorDto() { Id = f.Owner.Id, GlobalId = f.Owner.GlobalId }
                        });
                }

                int skipCount = (page - 1) * count;
                if (resultFolder.Count() <= skipCount)
                {
                    skipCount -= resultFolder.Count();
                    resultFolder = new List<FolderUnitDto>();
                    resultFiles = resultFiles.Skip(skipCount).Take(count);
                }
                else
                {
                    resultFolder = resultFolder.Skip(skipCount).Take(count);
                    count -= resultFolder.Count();
                    resultFiles = resultFiles.Take(count);
                }

                var owners = (await _userService.GetAllAsync()).Select(f => new { Id = f.id, Name = f.name });

                Parallel.ForEach(resultFiles, file =>
                {
                    file.Author.Name = owners.FirstOrDefault(o => o.Id == file.Author.GlobalId)?.Name;
                });
                Parallel.ForEach(resultFolder, folder =>
                {
                    folder.Author.Name = owners.FirstOrDefault(o => o.Id == folder.Author.GlobalId)?.Name;
                });


            }
            catch (Exception ex)
            {
                _logger.WriteError(ex, ex.Message);
            }
            return new SearchResultDto { Folders = resultFolder.ToList(), Files = resultFiles.ToList() };
        }

        public async Task<int> NumberOfFoundFoldersAndFilesAsync(int spaceId, int? folderId, string text)
        {
            int counter = 0;
            try
            {
                if (folderId != null)
                {
                    var folder = await _unitOfWork.Folders.Query.Where(f => f.Id == folderId)
                        .Select(s => new
                        {
                            Folders = s.DataUnits.OfType<FolderUnit>(),
                            Files = s.DataUnits.OfType<FileUnit>()
                        }).SingleOrDefaultAsync();
                    if (folder == null)
                        return 0;
                    counter += folder.Folders
                        .Count(f => f.Name.ToLower().Contains(text.ToLower()));

                    counter += folder.Files
                        .Count(f => f.Name.ToLower().Contains(text.ToLower()));
                }
                else
                {
                    var space = await _unitOfWork.Spaces.Query
                        .Where(s => s.Id == spaceId)
                        .Select(s => new
                        {
                            Folders = s.ContentList.OfType<FolderUnit>().Where(f => f.Parent == null),
                            Files = s.ContentList.OfType<FileUnit>().Where(f => f.Parent == null)
                        }).SingleOrDefaultAsync();
                    if (space == null)
                        return 0;
                    counter += space.Folders
                        .Count(f => f.Name.ToLower().Contains(text.ToLower()));
                    counter += space.Files
                        .Count(f => f.Name.ToLower().Contains(text.ToLower()));
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex, ex.Message);
            }
            return counter;
        }

        public void Dispose()
        {
            _unitOfWork?.Dispose();
        }
    }
}