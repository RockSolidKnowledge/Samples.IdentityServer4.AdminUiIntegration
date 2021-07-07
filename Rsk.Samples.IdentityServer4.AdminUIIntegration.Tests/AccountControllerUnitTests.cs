﻿using System.Threading.Tasks;
using IdentityExpress.Identity;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Controllers;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Models;
using Xunit;

namespace AdminUIIntegration.Tests
{
    public class AccountControllerUnitTests
    {
        private readonly Mock<UserManager<IdentityExpressUser>> mockUserManager;
        private readonly Mock<IIdentityServerInteractionService> mockInteraction;
        private readonly Mock<IClientStore> mockClientStore;
        private readonly Mock<IHttpContextAccessor> mockAccessor;
        private readonly Mock<IAuthenticationSchemeProvider> mockSchemeProvider;
        private readonly Mock<IEventService> mockEvents;
        private readonly Mock<IUrlHelperFactory> mockUrlHelper;

        public AccountControllerUnitTests()
        {
            mockInteraction = new Mock<IIdentityServerInteractionService>();
            mockClientStore = new Mock<IClientStore>();
            mockAccessor = new Mock<IHttpContextAccessor>();
            var userStoreMock = new Mock<IUserStore<IdentityExpressUser>>();
            mockUserManager = new Mock<UserManager<IdentityExpressUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
            mockSchemeProvider = new Mock<IAuthenticationSchemeProvider>();
            mockEvents = new Mock<IEventService>();
            mockUrlHelper = new Mock<IUrlHelperFactory>();
        }

        //Returns view when modelState is invalid.
        [Fact]
        public async Task Register_WhenModelStateIsInvalid_()
        {
            //arrange
            var controller = new AccountController(mockInteraction.Object, mockClientStore.Object, mockAccessor.Object, mockUserManager.Object, mockSchemeProvider.Object, mockEvents.Object, mockUrlHelper.Object);
            var model = new RegisterInputModel();
            controller.ModelState.AddModelError("", "Unit Test Error");
            //act
            var result = await controller.Register(model);

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.Equal(1, viewResult.ViewData.ModelState.ErrorCount);
        }

        [Fact]
        public async Task Register_WhenUserCannotBeFound_ExpectModelStateHassInvalidUsernameErrorMessage()
        {
            //arrange
            var controller = new AccountController(mockInteraction.Object, mockClientStore.Object, mockAccessor.Object, mockUserManager.Object, mockSchemeProvider.Object, mockEvents.Object, mockUrlHelper.Object);


            mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(null as IdentityExpressUser);

            var model = new RegisterInputModel {
                Username = "Hello"
            };

            //act
            var result = await controller.Register(model);

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.Single(viewResult.ViewData.ModelState);
            Assert.Equal("Username does not exist", viewResult.ViewData.ModelState.Root.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Register_WhenAddPasswordFails_ExpectUpdateToNotBeCalledAndModelStateToHaveErrors()
        {
            //arrange
            var controller = new AccountController(mockInteraction.Object, mockClientStore.Object, mockAccessor.Object, mockUserManager.Object, mockSchemeProvider.Object, mockEvents.Object, mockUrlHelper.Object);

            mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new IdentityExpressUser());
            mockUserManager.Setup(x => x.AddPasswordAsync(It.IsAny<IdentityExpressUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Failed(new IdentityError {Description = "Error" }));
           
            var model = new RegisterInputModel
            {
                Username = "Hello"
            };

            //act
            var result = await controller.Register(model);
            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<IdentityExpressUser>()), Times.Never);
        }

        [Fact]
        public async Task Register_WhenAddPasswordSuccees_ExpectUpdateToHaveBeenCalled()
        {
            //arrange
            var controller = new AccountController(mockInteraction.Object, mockClientStore.Object, mockAccessor.Object, mockUserManager.Object, mockSchemeProvider.Object, mockEvents.Object, mockUrlHelper.Object);
            mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new IdentityExpressUser());
            mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<IdentityExpressUser>())).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.AddPasswordAsync(It.IsAny<IdentityExpressUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var model = new RegisterInputModel
            {
                Username = "Hello"
            };

            //act
            var result = await controller.Register(model);
            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(viewResult.ViewData.ModelState.IsValid);
            mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<IdentityExpressUser>()), Times.Once);
        }
    }
}
