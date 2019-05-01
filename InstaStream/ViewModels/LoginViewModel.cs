using System;
using System.IO;
using System.Text.RegularExpressions;
using Caliburn.Micro;
using InstagramApiSharp.Classes;
using InstaStream.Models;
using MaterialDesignThemes.Wpf;

namespace InstaStream.ViewModels
{
    public class LoginViewModel : AbstractPage
    {
        private string _username;
        private string _password;
        private string _challengeValue;
        private string _challengeCode;

        private const string LOGIN_DIALOG = "LoginDialog";
        private const string ERROR_DIALOG = "ErrorDialog";
        
        public string Username
        {
            get => _username;
            set { _username = value; NotifyOfPropertyChange(); }
        }
        public string Password
        {
            get => _password;
            set { _password = value; NotifyOfPropertyChange(); }
        }

        public string ChallengeValue
        {
            get => _challengeValue;
            set { _challengeValue = value; NotifyOfPropertyChange();}
        }

        public string ChallengeCode
        {
            get => _challengeCode;
            set { _challengeCode = value; NotifyOfPropertyChange();}
        }

        public LoginViewModel(IHostScreen parent, InstagramSession session) : base(parent, session)
        {
            if (!session.HasUserData) return;
            
            Username = session.Settings.Username;
            Password = session.Settings.Password;
        }

        public async void Login()
        {
            parent.IsLoading = true;
            InstagramApiSharp.Classes.IResult<InstaLoginResult> res = await session.Login(UserSessionData.ForUsername(Username).WithPassword(Password));
            parent.IsLoading = false;
            if (res.Succeeded)
            {
                LoggedIn();
                return;
            }

            if (res.Info.NeedsChallenge)
            {
                parent.IsLoading = true;
                InstagramApiSharp.Classes.IResult<InstaChallengeRequireVerifyMethod> challenge = await session.API.GetChallengeRequireVerifyMethodAsync();
                parent.IsLoading = false;
                if (!challenge.Succeeded) return;
                
                if (challenge.Value.SubmitPhoneRequired)
                {
                    //submit
                    await DialogHost.Show(new ErrorMessageBox
                        {Message = "You need to link your phone to your Instagram-Account!"}, ERROR_DIALOG);
                }
                else if (challenge.Value.StepData != null)
                {
                    CodeDialogViewModel cdv;
                    if (!string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
                    {
                        parent.IsLoading = true;
                        InstagramApiSharp.Classes.IResult<InstaChallengeRequireSMSVerify> phoneNumber = await session.API.RequestVerifyCodeToSMSForChallengeRequireAsync();
                        parent.IsLoading = false;
                        if (!phoneNumber.Succeeded) return;
                            
                        cdv = new CodeDialogViewModel
                        {
                            Message = $"A code was sent to {phoneNumber.Value.StepData.ContactPoint}. Type the code here:"
                        };
                            

                    }else if (!string.IsNullOrEmpty(challenge.Value.StepData.Email))
                    {
                        parent.IsLoading = true;
                        InstagramApiSharp.Classes.IResult<InstaChallengeRequireEmailVerify> email = await session.API.RequestVerifyCodeToEmailForChallengeRequireAsync();
                        parent.IsLoading = false;
                        if (!email.Succeeded) return;
                            
                        cdv = new CodeDialogViewModel
                        {
                            Message = $"A code was sent to {email.Value.StepData.ContactPoint}. Type the code here:"
                        };
                    }
                    else
                    {
                        await DialogHost.Show(new ErrorMessageBox {Message = "Could not find any verification method"}, ERROR_DIALOG);
                        return;
                    }

                    while (true)
                    {
                        await DialogHost.Show(cdv, LOGIN_DIALOG);
                        cdv.Code = cdv.Code.Trim().Replace(" ", "");
                        Regex regex = new Regex(@"^-*[0-9,\.]+$");
                        if (regex.IsMatch(cdv.Code))
                        {
                            parent.IsLoading = true;
                            InstagramApiSharp.Classes.IResult<InstaLoginResult> verify =
                                await session.API.VerifyCodeForChallengeRequireAsync(ChallengeCode);
                            parent.IsLoading = false;
                            if (verify.Succeeded)
                            {
                                LoggedIn();
                                break;
                            }

                            await DialogHost.Show(new ErrorMessageBox {Message = "Could not verify the code"}, ERROR_DIALOG);
                        }
                        else
                        {
                            await DialogHost.Show(new ErrorMessageBox {Message = "The code is alphanumeric"},
                                ERROR_DIALOG);
                        }
                    }
                }
            }else if (res.Value == InstaLoginResult.TwoFactorRequired)
            {
                CodeDialogViewModel cdv = new CodeDialogViewModel
                {
                    Message = "Type your two-facotor-auth-code here:"
                };
                await DialogHost.Show(cdv, LOGIN_DIALOG);
                parent.IsLoading = true;
                InstagramApiSharp.Classes.IResult<InstaLoginTwoFactorResult> tfa = await session.API.TwoFactorLoginAsync(cdv.Code);
                parent.IsLoading = false;
                if (tfa.Succeeded)
                {
                    LoggedIn();
                }
                else
                {
                    await DialogHost.Show(new ErrorMessageBox {Message = "Could not verify the code"}, ERROR_DIALOG);
                }
            }
            else
            {
                await DialogHost.Show(new ErrorMessageBox {Message = "Could not login"}, ERROR_DIALOG);
            }
        }

        private void LoggedIn()
        {
            session.LoggedIn = true;
            parent.ShowPage(new StartStreamViewModel(parent, session));
        }
    }
    
    public class CodeDialogViewModel : PropertyChangedBase
    {
        private string _message;
        private string _code;

        public string Message
        {
            get => _message;
            set { _message = value; NotifyOfPropertyChange(); }
        }

        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                NotifyOfPropertyChange();
            }
        }

        public CodeDialogViewModel()
        {
            Code = string.Empty;
        }
    }

    public class ErrorMessageBox : PropertyChangedBase
    {
        private string _message;
        public string Message
        {
            get => _message;
            set { _message = value; NotifyOfPropertyChange(); }
        }
    }
}