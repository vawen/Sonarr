﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Common.Extensions
{
    public static class UrlExtensions
    {
        public static bool IsValidUrl(this string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            Uri uri;
            if (!Uri.TryCreate(path, UriKind.Absolute, out uri))
            {
                return false;
            }

            if (!uri.IsWellFormedOriginalString())
            {
                return false;
            }

            return true;

        }
    }
}
