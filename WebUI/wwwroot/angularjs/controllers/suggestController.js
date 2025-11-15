app
    .directive('fileAutoUpload', ['$parse', function ($parse) {
        return {
            restrict: 'A',
            link: function (scope, element, attrs) {
                const fn = $parse(attrs.fileAutoUpload); // self.send(file)
                element.bind('change', function () {
                    const file = element[0].files[0];
                    if (file) {
                        scope.$apply(function () {
                            fn(scope, { file: file });
                        });
                    }
                });
            }
        };
    }])
    .controller('suggestController', ['$scope', '$routeParams', '$rootScope', '$http', 'ajaxService', '$location', 'NgTableParams', 'Upload', '$timeout',
        function ($scope, $routeParams, $rootScope, $http, ajaxService, $location, NgTableParams, Upload, $timeout) {
            "use strict";
            var self = this;
            self.selectText = 'Select All';
            self.readyToSend = [];

            self.item = {
                ResponseText: '',
                SuggestId: '',
            };
            self.resetItem = function () {
                self.item = {
                    ResponseText: '',
                    SuggestId: '',
                };
            };
            self.initializeController = function () {
                ajaxService.WhoseMenu();

                self.jobTypes = jobType;

                if ($location.path().includes("/user/jobTracker")) {
                    //self.getAllNewJobsByUserId();
                    self.getAllWaitingJobsByUserId();
                    self.getAllSentSuggestByUserId();
                }
            };

            self.updateQuestions = function () {
                const model = {
                    SuggestId: self.item.SuggestId,
                    Questions: self.Questions.map((obj) => {
                        return {
                            Id: obj.Id,
                            Answer: obj.Answer
                        };
                    }),
                }
                return ajaxService.ajaxPost($rootScope.domain + "/api/Suggest/UpdateQuestions", model).then(function (data) {
                    if (data.status != 200)
                        return;
                    location.reload();
                });
            };
            self.addResponse = function () {
                const model = {
                    ResponseText: self.item.ResponseText,
                    SuggestId: self.item.SuggestId,
                }
                return ajaxService.ajaxPost($rootScope.domain + "/api/Suggest/AddResponse", model).then(function (data) {
                    if (data.status != 200)
                        return;
                    location.reload();
                });
            };
            self.getRow = function (row) {
                self.item.SuggestId = row.Id;
                self.Responses = row.Responses;
                self.CoverLetter = row.CoverLetter;
                self.Job = row;

                self.Questions = row.Questions;
            };
            self.getAllSentSuggestByUserId = function () {
                return ajaxService.ajaxGet($rootScope.domain + "/api/Suggest/GetAllSentSuggestByUserId").then(function (data) {
                    if (data.status != 200)
                        return;
                    self.suggests = data.data;
                });
            };
            self.getAllWaitingJobsByUserId = function () {
                return ajaxService.ajaxGet($rootScope.domain + "/api/Suggest/GetAllWaitingJobsByUserId").then(function (data) {
                    if (data.status != 200)
                        return;
                    self.waitingJobs = data.data;
                });
            };
            self.getAllNewJobsByUserId = function () {
                return ajaxService.ajaxGet($rootScope.domain + "/api/Suggest/GetAllNewJobsByUserId").then(function (data) {
                    if (data.status != 200)
                        return;
                    self.newJobs = data.data;
                });
            };
            self.selectAllJobs = function () {
                if (self.selectText == 'Select All') {
                    self.newJobs.forEach(function (item) {
                        item.IsSelected = true;
                    });

                    self.selectText = 'Unselect All';
                }
                else {
                    self.newJobs.forEach(function (item) {
                        item.IsSelected = false;
                    });

                    self.selectText = 'Select All';
                }
            };
            self.selectJob = function (row) {
                //row.IsSelected = !row.IsSelected;
                window.open(row.Link, '_blank');
            };
            self.sendApplication = function () {
                const model = {
                    Title: self.Job.Title,
                    Company: self.Job.Company,
                    Location: self.Job.Location,
                    Link: self.Job.Link,
                    CoverLetter: self.Job.CoverLetter,
                    JobType: self.Job.Type,
                }
                return ajaxService.ajaxPost($rootScope.domain + "/api/Suggest/SendApplication", model).then(function (data) {
                    if (data.status != 200)
                        return;

                    if ($location.path().includes("/user/jobTracker"))
                        location.reload();
                    else {
                        SuccessMsg('Application Send Successfully');
                        $('#coverLetterModal').modal('hide');
                    }
                });
            };
            self.addToReady = function () {
                self.readyToSend = self.newJobs.filter(m => m.IsSelected);

                const model = {
                    Jobs: self.readyToSend,
                }
                return ajaxService.ajaxPost($rootScope.domain + "/api/Suggest/GenerateCoverLetter", model).then(function (data) {
                    if (data.status != 200)
                        return;
                    SuccessMsg('CoverLetter Generate Successfully. see in Ready To Send tab');

                    self.readyToSend = data.data;
                });
            }
            //
            self.result = [];
            self.send = async function (file) {
                if (!file) { return; }

                const fd = new FormData();
                fd.append("pdf", file);
                fd.append("TokenId", ajaxService.getCookie('TokenId'));

                try {
                    self.startChangeTabs(5500);

                    $http.post('/api/Suggest/GenerateResumeAndGetNewJobs', fd, { headers: { 'Content-Type': undefined } })
                        .then(function (resp) {
                            self.result = resp.data;
                        });
                } catch (err) {
                    console.error(err);
                }
            };
            self.startChangeTabs = async function (seconds) {
                self.changeTabs(2);
                await new Promise(resolve => setTimeout(resolve, seconds));

                self.changeTabs(3);
                await new Promise(resolve => setTimeout(resolve, seconds));

                self.changeTabs(4);
                await new Promise(resolve => setTimeout(resolve, seconds));

                self.changeTabs(5);
            };
            self.changeTabs = function (second) {
                document.getElementById('tab1').classList.remove('active');
                document.getElementById('tab2').classList.remove('active');
                document.getElementById('tab3').classList.remove('active');
                document.getElementById('tab4').classList.remove('active');
                document.getElementById('tab5').classList.remove('active');

                if (second == 2) {
                    document.getElementById('tab2').classList.remove('inactive');
                    document.getElementById('tab2').classList.add('active');
                }
                else if (second == 3) {
                    document.getElementById('tab3').classList.remove('inactive');
                    document.getElementById('tab3').classList.add('active');
                }
                else if (second == 4) {
                    document.getElementById('tab4').classList.remove('inactive');
                    document.getElementById('tab4').classList.add('active');
                }
                else if (second == 5) {
                    document.getElementById('tab5').classList.remove('inactive');
                    document.getElementById('tab5').classList.add('active');
                }
            };
            ///
        }]);
