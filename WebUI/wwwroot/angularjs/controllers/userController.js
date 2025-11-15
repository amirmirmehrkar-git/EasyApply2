app.controller('userController', ['$scope', '$routeParams', '$rootScope', '$http', 'profileService', 'ajaxService', '$location', 'NgTableParams', 'Upload', '$timeout',
    function ($scope, $routeParams, $rootScope, $http, profileService, ajaxService, $location, NgTableParams, Upload, $timeout) {
        "use strict";
        var self = this;
        self.btnDownload = false;
        self.item = {
            FirstName: '',
            LastName: '',
            Email: '',
            PhoneNumber: '',
            Password: '',
            NeededJob: '',
            LinkedInUserName: '',
            LinkedInPassword: '',
            StepStoneUserName: '',
            StepStonePassword: '',
            XingUserName: '',
            XingPassword: '',
            ApplyType: 0,
        };
        self.resetItem = function () {
            self.Id = undefined;
            self.item = {
                FirstName: '',
                LastName: '',
                Email: '',
                PhoneNumber: '',
                Password: '',
                NeededJob: '',
                LinkedInUserName: '',
                LinkedInPassword: '',
                StepStoneUserName: '',
                StepStonePassword: '',
                XingUserName: '',
                XingPassword: '',
                ApplyType: 0,
            };
        };
        self.initializeController = function () {
            ajaxService.WhoseMenu();

            self.applyTypes = applyType.Labels;

            if ($location.path().includes("/user/setting")) {
                self.getProfile();
            }
        };
        self.tableParams = new NgTableParams($rootScope.tableParams["userController_tableParams"] ||
        {
            page: 1,
            count: 15,
        },
            {
                filterDelay: 300,
                getData: function (params) {
                    $rootScope.tableParams["userController_tableParams"] = params._params;

                    let url = "";
                    if ($location.path().includes("/admin/admins"))
                        url = $rootScope.domain + "/api/User/GetAllAdmins";
                    else if ($location.path().includes("/admin/users"))
                        url = $rootScope.domain + "/api/User/GetAllUsers";

                    return ajaxService.ajaxPaged(url, params._params).then(function (data) {
                        if (data.status != 200)
                            return;
                        params.total(data.data.count);
                        return data.data.items;
                    });
                }
            });

        self.tableParamsGetAllResume = new NgTableParams($rootScope.tableParams["userController_tableParams"] ||
        {
            page: 1,
            count: 15,
        },
            {
                filterDelay: 300,
                getData: function (params) {
                    $rootScope.tableParams["userController_tableParams"] = params._params;

                    let url = $rootScope.domain + "/api/Document/GetAllResumeByUserId";

                    return ajaxService.ajaxPaged(url, params._params).then(function (data) {
                        if (data.status != 200)
                            return;
                        params.total(data.data.count);
                        return data.data.items;
                    });
                }
            });

        self.addOrUpdateUser = function () {
            if (!$scope.formAddOrUpdateUser.$valid) {
                return;
            }
            const model = {
                Id: self.Id ? self.Id : emptyGuid,
                FirstName: self.item.FirstName,
                LastName: self.item.LastName,
                PhoneNumber: self.item.PhoneNumber,
                Password: self.item.Password,
                Shba: self.item.Shba,
                BankAccount: self.item.BankAccount,
                CardNumber: self.item.CardNumber,
                Active: chkActive.checked == true ? true : false,
            }
            return ajaxService.ajaxPost($rootScope.domain + "/api/User/AddOrUpdateUser", model).then(function (data) {
                if (data.status != 200)
                    return;
                location.reload();
            });
        };

        self.getRow = function (row) {
            self.Id = row.Id;
            self.item.FirstName = row.FirstName;
            self.item.LastName = row.LastName;
            self.item.Email = row.Email;
            self.item.PhoneNumber = row.PhoneNumber;
            self.item.Password = row.Password;
        };
        self.delete = function () {
            return ajaxService.ajaxGet($rootScope.domain + "/api/User/Delete/" + self.Id).then(function (data) {
                if (data.status != 200)
                    return;
                location.reload();
            });
        };
        self.loginWithToken = function (user) {
            $rootScope.spinnerActive = true;
            $http.get($rootScope.domain + "/api/account/loginWithToken/" + user.Id).then(function (result) {
                const data = result.data;
                if (data && data.success) {

                    document.cookie = "TokenId=" + data.TokenId;
                    document.cookie = "Token=" + data.Token;
                    document.cookie = "UserRole=" + data.role;

                    profileService.refreshProfile();

                    if (data.role == 'Admin') {
                        window.location.replace('#!/admin/dashboard');
                    }
                    else if (data.role == 'User') {
                        window.location.replace('#!/user/dashboard');
                    }
                }
                else {
                    self.modelState = data.errors;
                    $rootScope.spinnerActive = false;
                }
            }, function (response, failed) {
                $rootScope.spinnerActive = false;
                if (response.status == 400) {
                    self.modelState = response.data.ModelState.Messages;
                    return;
                }
                if (response.status != 200)
                    return;
            });
        };
        self.updateProfile = function () {
            const model = {
                FirstName: self.item.FirstName,
                LastName: self.item.LastName,
                NeededJob: self.item.NeededJob,
                PhoneNumber: self.item.PhoneNumber,
                AboutMe: self.item.AboutMe,
                ImageName: self.item.ImageName,
                LinkedInUserName: self.item.LinkedInUserName,
                LinkedInPassword: self.item.LinkedInPassword,
                StepStoneUserName: self.item.StepStoneUserName,
                StepStonePassword: self.item.StepStonePassword,
                XingUserName: self.item.XingUserName,
                XingPassword: self.item.XingPassword,
                ApplyType: self.item.ApplyType,
            }
            return ajaxService.ajaxPost($rootScope.domain + "/api/User/UpdateProfile", model).then(function (data) {
                if (data.status != 200)
                    return;
                location.reload();
            });
        };
        self.getProfile = function () {
            return ajaxService.ajaxGet($rootScope.domain + "/api/Account/GetProfile/").then(function (data) {
                if (data.status != 200)
                    return;
                self.item = {
                    ...data.data
                };
            });
        };
        self.addOrUpdateResume = function () {
            const model = {
                FileName: self.FileName,
            }
            return ajaxService.ajaxGet($rootScope.domain + "/api/Document/AddOrUpdateResume/" + self.FileName).then(function (data) {
                if (data.status != 200)
                    return;
                location.reload();
            });
        };
        self.getRow = function (row) {
            self.Id = row.Id;
        };
        self.delete = function () {
            return ajaxService.ajaxGet($rootScope.domain + "/api/Document/Delete/" + self.Id).then(function (data) {
                if (data.status != 200)
                    return;
                location.reload();
            });
        };
        $scope.Upload = function (files) {
            $scope.SelectedFiles = files;
            if ($scope.SelectedFiles && $scope.SelectedFiles.length) {
                Upload.upload({
                    url: $rootScope.domain + '/File/UploadResume/',
                    data: {
                        files: $scope.SelectedFiles
                    }
                }).then(function (response) {
                    $timeout(function () {
                        $scope.Result = response.data;
                        self.FileName = response.data;
                    });
                }, function (response) {
                    if (response.status > 0) {
                        var errorMsg = response.status + ': ' + response.data;
                        alert(errorMsg);
                    }
                }, function (evt) {
                    var element = angular.element(document.querySelector('#dvProgress1'));
                    $scope.Progress = Math.min(100, parseInt(100.0 * evt.loaded / evt.total));
                    element.html('<div style="width: ' + $scope.Progress + '%">' + $scope.Progress + '%</div>');
                });
            }
        };
        $scope.UploadUserImage = function (files) {
            $scope.SelectedFiles = files;
            if ($scope.SelectedFiles && $scope.SelectedFiles.length) {
                Upload.upload({
                    url: $rootScope.domain + '/File/UploadProfile/',
                    data: {
                        files: $scope.SelectedFiles
                    }
                }).then(function (response) {
                    $timeout(function () {
                        $scope.Result = response.data;
                        self.item.ImageName = response.data;
                    });
                }, function (response) {
                    if (response.status > 0) {
                        var errorMsg = response.status + ': ' + response.data;
                        alert(errorMsg);
                    }
                }, function (evt) {
                });
            }
        };
        self.downloadResume = function () {
            window.open($rootScope.domain + "/DownloadResume?Email=" + self.item.Email, '_blank');
        };
        self.selectTemplate = function (id) {
            if (document.getElementById(id).style.border.includes('dotted')) {
                document.getElementById(id).style.border = '2px solid darkorange';
                self.btnDownload = true;
            }
            else {
                document.getElementById(id).style.border = '2px dotted darkorange';
                self.btnDownload = false;
            }
        };
    }]);
