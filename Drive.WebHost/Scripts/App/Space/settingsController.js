﻿(function () {
    "use strict";

    angular.module("driveApp")
        .controller("SettingsController", SettingsController);

    SettingsController.$inject = ['SettingsService', 'SpaceService', 'FolderService', 'FileService', '$routeParams', '$location', '$rootScope', '$window', 'toastr', '$timeout'];

    function SettingsController(settingsService, spaceService, folderService, fileService, $routeParams, $location, $rootScope, $window, toastr, $timeout) {
        var vm = this;
        vm.save = save;
        vm.cancel = cancel;
        vm.addSpaceUser = addSpaceUser;
        vm.addSpaceRole = addSpaceRole;
        vm.addReadUser = addReadUser;
        vm.addWriteUser = addWriteUser;
        vm.addReadRole = addReadRole;
        vm.addWriteRole = addWriteRole;
        vm.removeSpaceUser = removeSpaceUser;
        vm.removeSpaceRole = removeSpaceRole;
        vm.selectedSpace = $routeParams.id ? $routeParams.id : 1;
        vm.redirectToSpace = redirectToSpace;
        vm.space = {
            readPermittedUsers: [],
            modifyPermittedUsers: [],
            readPermittedRoles: [],
            modifyPermittedRoles: []
        }
        vm.permittedUsers = [];
        vm.permittedRoles = [];
        vm.tab = 1;
        vm.setTab = setTab;
        vm.isSet = isSet;
        vm.deleteSpace = deleteSpace;
        vm.addAllUsers = addAllUsers;
        vm.allModify = false;

        activate();

        function addAllUsers() {
            vm.permittedUsers = [];
            for (var i = 0; i < vm.users.length; i++) {
                var pos = vm.permittedUsers.map(function (e) { return e.globalId; }).indexOf(vm.users[i].id);
                if (pos == -1) {
                    vm.permittedUsers.push({
                        name: vm.users[i].name,
                        globalId: vm.users[i].id,
                        confirmedRead: true,
                        confirmedWrite: vm.allModify
                    });
                }
                addReadUser(true, vm.users[i].id);
                if (vm.allModify) {
                    addWriteUser(true, vm.users[i].id);
                }
                else {
                    vm.space.modifyPermittedUsers = [];
                }
            }
        }

        function activate() {
            settingsService.getSpace(vm.selectedSpace, function (data) {
                vm.space = data;
                // Hide delete space btn for Binary and My spaces
                if (vm.space.type === 0 || vm.space.type === 1) {
                    vm.showDeleteBtn = false;
                }
                else {
                    vm.showDeleteBtn = true;
                }
                console.log(vm.space);
                settingsService.getAllUsers(function (data) {
                    vm.users = data;
                    if (vm.space.readPermittedUsers != undefined) {
                        for (var i = 0; i < vm.space.readPermittedUsers.length; i++) {
                            for (var j = 0; j < vm.users.length; j++) {
                                if (vm.space.readPermittedUsers[i].globalId === vm.users[j].id) {
                                    vm.permittedUsers.push({
                                        name: vm.users[j].name,
                                        globalId: vm.space.readPermittedUsers[i].globalId,
                                        confirmedRead: true
                                    });
                                    break;
                                }
                            }
                        }
                    }
                    vm.bool = true;
                    if (vm.space.modifyPermittedUsers != null) {
                        for (var i = 0; i < vm.space.modifyPermittedUsers.length; i++) {
                            vm.bool = true;
                            for (var j = 0; j < vm.permittedUsers.length; j++) {
                                if (vm.space.modifyPermittedUsers[i].globalId === vm.permittedUsers[j].globalId) {
                                    vm.permittedUsers[j].confirmedWrite = true;
                                    vm.bool = false;
                                }
                            }
                            if (vm.bool) {
                                for (var j = 0; j < vm.users.length; j++) {
                                    if (vm.space.modifyPermittedUsers[i].globalId === vm.users[j].id) {
                                        vm.permittedUsers.push({
                                            name: vm.users[j].name,
                                            globalId: vm.space.modifyPermittedUsers[i].globalId,
                                            confirmedWrite: true
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                });
                settingsService.getAllRoles(function (data) {
                    vm.roles = data;
                    if (vm.space.readPermittedRoles != undefined) {
                        for (var i = 0; i < vm.space.readPermittedRoles.length; i++) {
                            for (var j = 0; j < vm.roles.length; j++) {
                                if (vm.space.readPermittedRoles[i].id === vm.roles[j].id) {
                                    vm.permittedRoles.push({
                                        name: vm.roles[j].name,
                                        id: vm.space.readPermittedRoles[i].id,
                                        confirmedRead: true
                                    });
                                    break;
                                }
                            }
                        }
                    }
                    vm.bool = true;
                    if (vm.space.modifyPermittedRoles != null) {
                        for (var i = 0; i < vm.space.modifyPermittedRoles.length; i++) {
                            vm.bool = true;
                            for (var j = 0; j < vm.permittedRoles.length; j++) {
                                if (vm.space.modifyPermittedRoles[i].id === vm.permittedRoles[j].id) {
                                    vm.permittedRoles[j].confirmedWrite = true;
                                    vm.bool = false;
                                }
                            }
                            if (vm.bool) {
                                for (var j = 0; j < vm.roles.length; j++) {
                                    if (vm.space.modifyPermittedRoles[i].id === vm.roles[j].id) {
                                        vm.permittedRoles.push({
                                            name: vm.roles[j].name,
                                            id: vm.space.modifyPermittedRoles[i].id,
                                            confirmedWrite: true
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                });
            });
        };

        function save() {
            settingsService.pushChanges(vm.space, function () {
                $rootScope.$emit("getSpacesInMenu", {});
            });

            vm.redirectToSpace();
        };

        function cancel() {
            settingsService.getSpace(vm.selectedSpace, function (data) {
                vm.space = data;
            });
            vm.redirectToSpace();
        };

        function addSpaceUser() {
            if (vm.selected.id != null) {
                if (vm.permittedUsers.find(x => x.globalId === vm.selected.id)) {
                    toastr.warning(
                   'User already exist in this space!', 'Space settings',
                   {
                       closeButton: true, timeOut: 5000
                   });
                    return;
                };
                vm.permittedUsers.push({
                    name: vm.selected.name,
                    globalId: vm.selected.id
                });
            }
        };

        function addSpaceRole() {
            if (vm.selectedRole.id != null) {
                if (vm.permittedRoles.find(x => x.id == vm.selectedRole.id)) {
                    toastr.warning(
                   'The role already exist in this space!', 'Space settings',
                   {
                       closeButton: true, timeOut: 5000
                   });
                    return;
                };
                vm.permittedRoles.push({
                    name: vm.selectedRole.name,
                    id: vm.selectedRole.id
                });
            }
        }

        function addReadUser(bool, id) {
            vm.space.readPermittedUsers = vm.space.readPermittedUsers || [];
            if (bool === true) {
                for (var i = 0; i < vm.permittedUsers.length; i++) {
                    if (vm.permittedUsers[i].globalId === id) {
                        vm.space.readPermittedUsers.push({
                            name: vm.permittedUsers[i].name,
                            globalId: vm.permittedUsers[i].globalId
                        });
                        break;
                    }
                }
            } else {
                for (var i = 0; i < vm.space.readPermittedUsers.length; i++) {
                    if (vm.space.readPermittedUsers[i].globalId === id) {
                        vm.space.readPermittedUsers.splice(i, 1);
                        break;
                    }
                }
            }
        }

        function addReadRole(bool, id) {
            vm.space.readPermittedRoles = vm.space.readPermittedRoles || [];
            if (bool === true) {
                for (var i = 0; i < vm.permittedRoles.length; i++) {
                    if (vm.permittedRoles[i].id === id) {
                        vm.space.readPermittedRoles.push({
                            name: vm.permittedRoles[i].name,
                            id: vm.permittedRoles[i].id
                        });
                        break;
                    }
                }
            } else {
                for (var i = 0; i < vm.space.readPermittedRoles.length; i++) {
                    if (vm.space.readPermittedRoles[i].id === id) {
                        vm.space.readPermittedRoles.splice(i, 1);
                        break;
                    }
                }
            }
        }

        function addWriteUser(bool, id) {
            vm.space.modifyPermittedUsers = vm.space.modifyPermittedUsers || [];
            if (bool === true) {
                for (var i = 0; i < vm.permittedUsers.length; i++) {
                    if (vm.permittedUsers[i].globalId === id) {
                        vm.space.modifyPermittedUsers.push({
                            name: vm.permittedUsers[i].name,
                            globalId: vm.permittedUsers[i].globalId
                        });
                        vm.permittedUsers[i].confirmedRead = true;
                        addReadUser(true, id);
                        break;
                    }
                }
            } else {
                for (var i = 0; i < vm.space.modifyPermittedUsers.length; i++) {
                    if (vm.space.modifyPermittedUsers[i].globalId === id) {
                        vm.space.modifyPermittedUsers.splice(i, 1);
                        break;
                    }
                }
            }
        }

        function addWriteRole(bool, id) {
            vm.space.modifyPermittedRoles = vm.space.modifyPermittedRoles || [];
            if (bool === true) {
                for (var i = 0; i < vm.permittedRoles.length; i++) {
                    if (vm.permittedRoles[i].id === id) {
                        vm.space.modifyPermittedRoles.push({
                            name: vm.permittedRoles[i].name,
                            id: vm.permittedRoles[i].id
                        });
                        vm.permittedRoles[i].confirmedRead = true;
                        addReadRole(true, id);
                        break;
                    }
                }
            } else {
                for (var i = 0; i < vm.space.modifyPermittedRoles.length; i++) {
                    if (vm.space.modifyPermittedRoles[i].id === id) {
                        vm.space.modifyPermittedRoles.splice(i, 1);
                        break;
                    }
                }
            }
        }

        function removeSpaceUser(id) {
            for (var i = 0; i < vm.permittedUsers.length; i++) {
                if (vm.permittedUsers[i].globalId === id) {
                    toastr.success(
                                 'User has been deleted!', 'Space settings',
                                 {
                                     closeButton: true, timeOut: 5000
                                 });
                    vm.permittedUsers.splice(i, 1);
                    break;
                }
            }
        };

        function removeSpaceRole(id) {
            for (var i = 0; i < vm.permittedRoles.length; i++) {
                if (vm.permittedRoles[i].id === id) {
                    toastr.success(
                                    'Role has been deleted!', 'Space settings',
                                    {
                                        closeButton: true, timeOut: 5000
                                    });
                    vm.permittedRoles.splice(i, 1);
                    break;
                }
            }
        };

        function redirectToSpace() {
            $location.url("/spaces/" + vm.space.id);
        };

        function setTab(newTab) {
            vm.tab = newTab;
        };

        function isSet(tabNum) {
            return vm.tab === tabNum;
        };

        function deleteSpace() {
            swal({
                title: "Deleting space!",
                text: "Are you sure that you want delete space and all folders and files in it?",
                type: "warning",
                showCancelButton: true,
                confirmButtonColor: "#DD6B55",
                confirmButtonText: "Yes, delete it!",
                closeOnConfirm: false
            }, function () {
                settingsService.deleteSpaceWithStaff(vm.selectedSpace, function (response) {
                    if (response) {
                        var data = {
                            operation: 'delete',
                            item: response
                        }
                    }
                });
                swal({
                    title: "Deleted!",
                    text: "Your space has been deleted.",
                    timer: 2000,
                    showConfirmButton: false,
                    type: "success"
                });
                $timeout(function () {
                    $window.location.reload(true);
                    $location.url("/");
                }, 2100);
            });
        }
    }
}());