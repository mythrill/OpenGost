﻿#if NET45
using System.Security.Cryptography;
using Xunit;

namespace OpenGost.Security.Cryptography
{
    public class GostECDsa256SignatureDescriptionTests : SignatureDescriptionTest<GostECDsa256SignatureDescription>
    {
        [Fact(DisplayName = nameof(ValidateCreateDigest))]
        public void ValidateCreateDigest()
        {
            using (HashAlgorithm digest = CreateDigest())
            {
                Assert.NotNull(digest);
                Assert.True(digest is Streebog256);
            }
        }

        [Fact(DisplayName = nameof(ValidateCreateDeformatter))]
        public void ValidateCreateDeformatter()
        {
            AsymmetricSignatureDeformatter deformatter = CreateDeformatter(GostECDsa256.Create());

            Assert.NotNull(deformatter);
            Assert.True(deformatter is GostECDsa256SignatureDeformatter);
        }

        [Fact(DisplayName = nameof(ValidateCreateFormatter))]
        public void ValidateCreateFormatter()
        {
            AsymmetricSignatureFormatter formatter = CreateFormatter(GostECDsa256.Create());

            Assert.NotNull(formatter);
            Assert.True(formatter is GostECDsa256SignatureFormatter);
        }
    }
} 
#endif
