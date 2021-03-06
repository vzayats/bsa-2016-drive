﻿(function () {
    "use strict";

    angular.module("driveApp")
        .controller("adminPanelController", AdminPanelController);

    AdminPanelController.$inject = ['AdminPanelService', '$uibModal', '$window'];

    function AdminPanelController(adminPanelService, $uibModal, $window) {
        var vm = this;
        vm.dirty = {};
        vm.createRole = createRole;
        vm.editRole = editRole;
        vm.removeRole = removeRole;
        vm.syncUsers = syncUsers;
        vm.tab = 1;
        vm.setTab = setTab;
        vm.isSet = isSet;
        activate();

        function activate() {
            vm.title = "Admin Panel";
            adminPanelService.getAllRoles(function (data) {
                vm.roles = data;
            });
        }

        function createRole(size) {
            var modalInstance = $uibModal.open({
                animation: true,
                templateUrl: 'Scripts/App/AdminPanel/Roles/CreateRole.html',
                windowTemplateUrl: 'Scripts/App/Space/Modal.html',
                controller: 'RoleController',
                controllerAs: 'roleCtrl',
                keyboard: true,
                size: size,
                resolve: {
                    items: function () {
                    },
                    parentvm: vm
                }
            });
            modalInstance.result.then(function (data) {
                vm.roles.push({
                    name: vm.roleName,
                    id: vm.roleId
                });
            }, function () {
            });
        };

        function editRole(size, id) {
            var modalInstance = $uibModal.open({
                animation: true,
                templateUrl: 'Scripts/App/AdminPanel/Roles/EditRole.html',
                windowTemplateUrl: 'Scripts/App/Space/Modal.html',
                controller: 'RoleController',
                controllerAs: 'roleCtrl',
                keyboard: true,
                size: size,
                resolve: {
                    items: function () {
                        return id;
                    },
                    parentvm: vm
                }
            });
            modalInstance.result.then(function () {
                adminPanelService.getAllRoles(function (data) {
                    vm.roles = data;
                });
            }, function () {
            });
        }

        function setTab(newTab) {
            vm.tab = newTab;
        };

        function isSet(tabNum) {
            return vm.tab === tabNum;
        };

        function removeRole(id) {
            swal({
                title: "Deleting role!",
                text: "Do you really want to delete role?",
                type: "warning",
                showCancelButton: true,
                confirmButtonColor: "#DD6B55",
                confirmButtonText: "Yes, delete it!",
                closeOnConfirm: false
            }, function () {
                adminPanelService.deleteRole(id, function (response) {
                    if (response) {
                        var data = {
                            operation: 'delete',
                            item: response
                        }
                    }
                });
                for (var i = 0; i < vm.roles.length; i++) {
                    if (vm.roles[i].id === id) {
                        vm.roles.splice(i, 1); break;
                    }
                }
                swal({
                    title: "Deleted!",
                    text: "Role has been deleted.",
                    timer: 2000,
                    showConfirmButton: false,
                    type: "success"
                });
            });
        }
        
        function syncUsers()
        {
            adminPanelService.syncUsers(function () {
                swal({
                    title: "Synchronized!",
                    text: "Users have been synchronized.",
                    timer: 2000,
                    showConfirmButton: false,
                    type: "success"
                });
            });
        }

    }
}());