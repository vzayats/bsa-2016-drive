﻿(function () {
    "use strict";

    angular
        .module("driveApp")
        .controller("FolderController", FolderController);

    FolderController.$inject = ['FolderService', '$uibModal', '$rootScope'];

    function FolderController(folderService, $uibModal, $rootScope) {
        var vm = this;
        vm.folder = {
            id: 0,
            isDeleted: false,
            name: '',
            description: '',
            createdAt: '',
            lastModified: ''
        };

        vm.id = 0;

        vm.folders = [];
        vm.getAll = getAll;
        vm.get = get;
        vm.deleteFolder = deleteFolder;
        vm.open = open;
        vm.create = create;
        vm.update = update;


        vm.menuOptions = [
            [
                'Share', function ($itemScope) {
                    console.log($itemScope.folder.id);
                }
            ],
            [
                'Edit', function ($itemScope) {
                    vm.folder = $itemScope.folder;
                    vm.open();
                }
            ],
            [
                'Delete', function ($itemScope) {
                    return deleteFolder($itemScope.folder.id);
                }
            ]
        ];

        vm.createOption = [
            [
                'Create', function ($itemScope) {
                },
                [
                    [
                        'Folder', function () {
                            vm.folder = {}
                            vm.open();
                        }
                    ],
                    [
                        'File', function ($itemScope) {

                        }
                    ]
                ]
            ]
        ];

        function open(size) {

            var modalInstance = $uibModal.open({
                animation: false,
                templateUrl: 'Scripts/App/Folder/Form.html',
                windowTemplateUrl: 'Scripts/App/Folder/Modal.html',
                controller: 'ModalInstanceCtrl',
                controllerAs: 'modalCtrl',
                size: size,
                resolve: {
                    items: function () {
                        return vm.folder;
                    }
                }
            });

            modalInstance.result.then(function (folder) {
                console.log(folder);
                if (folder.id == 0) {
                    vm.create(folder);
                } else {
                    vm.update(folder);
                }
            }, function () {
                console.log('Modal dismissed');
            });
        };

        function create(folder) {
            folderService.create(folder, function (response) {
                vm.folders.push(response);
            });
        }

        function update(folder) {
            folderService.updateFolder(folder, function (response) {
                for (var i = 0, len = vm.folders.length; i < len; i++) {
                    if (vm.folders[i].id == folder.id) {
                        vm.folders[i] = response.data;
                    }
                }
            });
        }

        activate();

        function activate() {
            return getAll();
        }

        function get(id) {
            folderService.get(id, function (folder) {
                vm.folder = folder;
            });
        }

        function getAll() {
            folderService.getAll(function (folders) {
                vm.folders = folders;
            });
        }



        function deleteFolder(id) {
            folderService.deleteFolder(id, function () {
                folderService.getAll(function (folders) {
                    vm.folders = folders;
                });
            });
        }
    }
}());