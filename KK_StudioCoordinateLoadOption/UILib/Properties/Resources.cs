using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace UILib.Properties
{
	// Token: 0x02000029 RID: 41
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class Resources
	{
		// Token: 0x0600014D RID: 333 RVA: 0x0000ED0C File Offset: 0x0000CF0C
		internal Resources()
		{
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x0600014E RID: 334 RVA: 0x0000ED14 File Offset: 0x0000CF14
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (Resources.resourceMan == null)
				{
					Resources.resourceMan = new ResourceManager("UILib.Properties.Resources", typeof(Resources).Assembly);
				}
				return Resources.resourceMan;
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x0600014F RID: 335 RVA: 0x0000ED44 File Offset: 0x0000CF44
		// (set) Token: 0x06000150 RID: 336 RVA: 0x0000ED4C File Offset: 0x0000CF4C
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return Resources.resourceCulture;
			}
			set
			{
				Resources.resourceCulture = value;
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000151 RID: 337 RVA: 0x0000ED54 File Offset: 0x0000CF54
		internal static byte[] DefaultResources
		{
			get
			{
				return (byte[])Resources.ResourceManager.GetObject("DefaultResources", Resources.resourceCulture);
			}
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000152 RID: 338 RVA: 0x0000ED70 File Offset: 0x0000CF70
		internal static byte[] DefaultResourcesKOI
		{
			get
			{
				return (byte[])Resources.ResourceManager.GetObject("DefaultResourcesKOI", Resources.resourceCulture);
			}
		}

		// Token: 0x040000EB RID: 235
		private static ResourceManager resourceMan;

		// Token: 0x040000EC RID: 236
		private static CultureInfo resourceCulture;
	}
}
