﻿angular.module('driveApp',
    ['ngRoute', 'ui.bootstrap.contextMenu', 'ui.bootstrap'])
    .config([
        '$routeProvider',
        function($routeProvider) {

            $routeProvider
                .when('/', // Home Page
                {
                    templateUrl: '/Scripts/App/Home/Home.html',
                    controller: 'HomeController',
                    controllerAs: 'homeCtrl'
                })
                //.when('/Home/Index', // Space page
                //{
                //    templateUrl: '/Scripts/App/Space/Space.html',
                //    controller: 'SpaceController',
                //    controllerAs: 'spaceCtrl'
                //})
                //.when('/', // Space settings Page
                //{
                //    templateUrl: '/Scripts/App/Space/Settings.html',
                //    controller: 'SettingsController',
                //    controllerAs: 'settingsCtrl'
                //})
                .when('/Folders', // Space settings Page
                {
                    templateUrl: '/Scripts/App/Folder/Folder.html',
                    controller: 'FolderController',
                    controllerAs: 'folderCtrl'
                })
                .otherwise({ // This is when any route not matched - error
                    controller: 'ErrorController'
                });
        }
    ]);