﻿// All code copyright (c) 2014 Ophidian Games || http://www.ophidian-games.com
// See http://www.losgap.com/ for licensing information
// Created on 03 11 2014 at 11:34 by Ben Bowen

using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ophidian.Losgap.Interop;

namespace Ophidian.Losgap.Rendering {
	/// <summary>
	/// Represents a 2-dimensional array of texels (width &amp; height) that can be used by the rendering pipeline.
	/// </summary>
	/// <remarks>
	/// You can create 2D textures by using the <see cref="TextureFactory"/> class, and setting parameters on the returned
	/// <see cref="Texture2DBuilder{TTexel}"/> before calling <see cref="Texture2DBuilder{TTexel}.Create"/>.
	/// </remarks>
	/// <typeparam name="TTexel">A <see cref="Extensions.IsBlittable">blittable</see> struct type that represents a single texel.
	/// Must be a valid <see cref="ITexel"/>.</typeparam>
	[SupportedUsages(ResourceUsage.Immutable, ResourceUsage.DiscardWrite, ResourceUsage.Write,
		ResourceUsage.StagingRead, ResourceUsage.StagingWrite, ResourceUsage.StagingReadWrite)]
	[SupportedGPUBindings(
		GPUBindings.DepthStencilTarget | GPUBindings.RenderTarget | GPUBindings.ReadableShaderResource | GPUBindings.WritableShaderResource
	)]
	public sealed unsafe class Texture2D<TTexel> : BaseTextureResource<TTexel, Texture2D<TTexel>, Texture2DBuilder<TTexel>>, ITexture2D
		where TTexel : struct, ITexel {
		private readonly uint width, height;
		private readonly bool isMultisampled;

		/// <summary>
		/// The width, in texels, of this texture.
		/// </summary>
		public uint Width {
			get { return width; }
		}

		/// <summary>
		/// The height, in texels, of this texture.
		/// </summary>
		public uint Height {
			get { return height; }
		}

		/// <summary>
		/// The size in texels of this texture.
		/// </summary>
		public override uint SizeTexels {
			get { return TextureUtils.GetSizeTexels(IsMipmapped, width, height); }
		}

		/// <summary>
		/// Returns true if this texture is multisampled.
		/// </summary>
		public bool IsMultisampled {
			get { return isMultisampled; }
		}

		internal Texture2D(ResourceHandle resourceHandle, ResourceUsage usage, ByteSize size, 
			GPUBindings permittedBindings, bool isArrayTexture, 
			bool isMipGenTarget, bool allowsDynamicDetail, uint width, uint height, 
			uint texelSizeBytes, bool isMipmapped, uint numMips, uint arrayIndex, bool isMultisampled) 
			: base(resourceHandle, usage, size, permittedBindings, isArrayTexture, 
			isMipGenTarget, allowsDynamicDetail, 
			texelSizeBytes, isMipmapped, numMips, arrayIndex) {
			this.width = width;
			this.height = height;
			this.isMultisampled = isMultisampled;
		}

		/// <summary>
		/// The width, in texels, of the requested mip.
		/// </summary>
		/// <param name="mipIndex">The index of the mip level whose width it is you wish to ascertain.</param>
		/// <returns>The width, in texels, of the given mip index.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint MipWidth(uint mipIndex) {
			Assure.LessThan(mipIndex, NumMips, "Mip index out of bounds.");
			return TextureUtils.GetDimensionForMipLevel(width, mipIndex);
		}

		/// <summary>
		/// The height, in texels, of the requested mip.
		/// </summary>
		/// <param name="mipIndex">The index of the mip level whose height it is you wish to ascertain.</param>
		/// <returns>The height, in texels, of the given mip index.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint MipHeight(uint mipIndex) {
			Assure.LessThan(mipIndex, NumMips, "Mip index out of bounds.");
			return TextureUtils.GetDimensionForMipLevel(height, mipIndex);
		}

		/// <summary>
		/// Gets the size of the given mip.
		/// </summary>
		/// <param name="mipIndex">The mip to get the size of. Must be less than <see cref="BaseTextureResource{TTexel,TTexture,TBuilder}.NumMips"/>.</param>
		/// <returns>The <see cref="ByteSize"/> of the given mip.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override ByteSize GetSize(uint mipIndex) {
			return TextureUtils.GetSize(TexelSizeBytes, false, MipWidth(mipIndex), MipHeight(mipIndex));
		}

		/// <summary>
		/// Gets the number of texels in the given mip.
		/// </summary>
		/// <param name="mipIndex">The mip to get the size of. Must be less than <see cref="BaseTextureResource{TTexel,TTexture,TBuilder}.NumMips"/>.</param>
		/// <returns>The number of texels in the given mip (e.g width * height * depth, where applicable).</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override uint GetSizeTexels(uint mipIndex) {
			return TextureUtils.GetSizeTexels(false, MipWidth(mipIndex), MipHeight(mipIndex));
		}

		/// <summary>
		/// Returns a <see cref="Texture2DBuilder{TTexel}"/> that has all its properties set
		/// to the same values as are used by this texture. A clone of this texture can then be created by calling
		/// <see cref="IResourceBuilder.Create"/> on the returned builder.
		/// </summary>
		/// <param name="includeData">If true, the <see cref="Texture2DBuilder{TTexel}.WithInitialData">initial data</see> of the 
		/// returned resource builder will be set to a copy of the data contained within this resource. Requires that this
		/// resource's <see cref="BaseResource.CanRead"/> property returns <c>true</c>.</param>
		/// <returns>A new <see cref="Texture2DBuilder{TTexel}"/>.</returns>
		/// <exception cref="ResourceOperationUnavailableException">Thrown if <paramref name="includeData"/> is <c>true</c>,
		/// but <see cref="BaseResource.CanRead"/> is <c>false</c>.</exception>
		public override Texture2DBuilder<TTexel> Clone(bool includeData = false) {
			if (includeData) {
				return new Texture2DBuilder<TTexel>(
					Usage,
					ReadAll(),
					PermittedBindings,
					Width,
					Height,
					IsMipmapped,
					IsMipGenTarget,
					IsGlobalDetailTarget,
					IsMultisampled
				);
			}
			else {
				return new Texture2DBuilder<TTexel>(
					Usage,
					null,
					PermittedBindings,
					Width,
					Height,
					IsMipmapped,
					IsMipGenTarget,
					IsGlobalDetailTarget,
					IsMultisampled
				);
			}
		}

		/// <summary>
		/// Copies a region of this texture to a region of the destination texture (to and from the specified 
		/// <paramref name="srcMipIndex"/> and <paramref name="dstMipIndex"/>).
		/// The source region is specified by the <paramref name="srcRegion"/> parameter; and the destination region as a box
		/// of equal dimensions, but with the upper-left corner specified by 
		/// <paramref name="destWriteOffsetX"/> and <paramref name="destWriteOffsetY"/>.
		/// </summary>
		/// <param name="dest">The destination resource. Must not be null.</param>
		/// <param name="srcRegion">The region of the selected <paramref name="srcMipIndex"/> to copy (in texels).</param>
		/// <param name="srcMipIndex">The source mip to copy from. Only one mip level may copied to/from at a time.
		/// If this texture is not mipmapped, you must supply a value of <c>0U</c>.</param>
		/// <param name="dstMipIndex">The destination mip to copy to. Does not need to be the same as <paramref name="srcMipIndex"/>,
		/// but take care to ensure you do not overwrite between differing mip levels.
		/// If the destination texture is not mipmapped, you must supply a value of <c>0U</c>.</param>
		/// <param name="destWriteOffsetX">The x-position in the <paramref name="dstMipIndex"/> to copy the data to.</param>
		/// <param name="destWriteOffsetY">The y-position in the <paramref name="dstMipIndex"/> to copy the data to.</param>
		/// <exception cref="ResourceOperationUnavailableException">Thrown if dest returns <c>false</c> for 
		/// <see cref="BaseResource.CanBeCopyDestination"/>.</exception>
		/// <exception cref="AssuranceFailedException">(Debug only) Thrown if the combination of provided parameters would involve copying
		/// from outside this texture, or copying to outside the destination texture.</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0",
			Justification = "The parameter is checked by the assurance.")]
		public void CopyTo(Texture2D<TTexel> dest, SubresourceBox srcRegion, uint srcMipIndex = 0U, uint dstMipIndex = 0U, 
			uint destWriteOffsetX = 0U, uint destWriteOffsetY = 0U) {
			Assure.LessThan(
				srcMipIndex, NumMips,
				"Can not copy from mip level " + srcMipIndex + ": Only " + NumMips + " present in source texture."
			);
			Assure.LessThan(
				dstMipIndex, dest.NumMips,
				"Can not copy to mip level " + dstMipIndex + ": Only " + dest.NumMips + " present in destination texture."
			);
			Assure.LessThan(
				srcRegion.Left,
				MipWidth(srcMipIndex),
				"Buffer overflow: Please ensure you are not attempting to copy from past the end of the source texture."
			);
			Assure.LessThanOrEqualTo(
				srcRegion.Right,
				MipWidth(srcMipIndex),
				"Buffer overflow: Please ensure you are not attempting to copy from past the end of the source texture."
			);
			Assure.LessThan(
				srcRegion.Top,
				MipHeight(srcMipIndex),
				"Buffer overflow: Please ensure you are not attempting to copy from past the end of the source texture."
			);
			Assure.LessThanOrEqualTo(
				srcRegion.Bottom,
				MipHeight(srcMipIndex),
				"Buffer overflow: Please ensure you are not attempting to copy from past the end of the source texture."
			);
			Assure.LessThanOrEqualTo(
				srcRegion.Width + destWriteOffsetX,
				dest.MipWidth(dstMipIndex),
				"Buffer overflow: Please ensure you are not attempting to copy to past the end of the destination texture."
			);
			Assure.LessThanOrEqualTo(
				srcRegion.Height + destWriteOffsetY,
				dest.MipHeight(dstMipIndex),
				"Buffer overflow: Please ensure you are not attempting to copy to past the end of the destination texture."
			);

			base.CopyTo(
				dest,
				srcRegion,
				GetSubresourceIndex(srcMipIndex),
				dest.GetSubresourceIndex(dstMipIndex),
				destWriteOffsetX,
				destWriteOffsetY,
				0U
			);
		}

		/// <summary>
		/// Performs a <see cref="ResourceUsage.DiscardWrite"/> on this buffer. A discard-write is a faster write that first discards
		/// the old data, then writes the new data.
		/// </summary>
		/// <param name="data">The data to write.
		/// <see cref="ArraySlice{T}.Length">Length</see> vertices will be copied from the given
		/// array slice. The copy will start from the specified <see cref="ArraySlice{T}.Offset">Offset</see> in the
		/// contained array.
		/// </param>
		/// <param name="mipIndex">The mip level to write to. Only one mip level may be written to at a time. If this texture
		/// is not mipmapped, you must supply a value of <c>0U</c>.</param>
		/// <param name="writeOffsetX">The X-dimension offset in to the selected <paramref name="mipIndex"/> to write the data to.</param>
		/// <param name="writeOffsetY">The Y-dimension offset in to the selected <paramref name="mipIndex"/> to write the data to.</param>
		/// <exception cref="ResourceOperationUnavailableException">Thrown if <see cref="BaseResource.CanDiscardWrite"/> is
		/// <c>false</c>.</exception>
		/// <exception cref="AssuranceFailedException">(Debug only) Thrown if the combination of supplied parameters would
		/// result in writing past the end of the texture in any dimension.</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "writeOffsetY+1",
			Justification = "Overflow is impossible if the assurances didn't fail.")]
		public void DiscardWrite(ArraySlice<TTexel> data, uint mipIndex = 0U, 
			uint writeOffsetX = 0U, uint writeOffsetY = 0U) {
			Assure.LessThan(
				mipIndex, NumMips,
				"Can not write to mip level " + mipIndex + ": Only " + NumMips + " present in texture."
			);
			Assure.LessThanOrEqualTo(
				data.Length + writeOffsetX + (writeOffsetY * Width),
				Width * Height,
				"Buffer overrun: Please ensure you are not attempting to write past the end of the texture."
			);

			ThrowIfCannotDiscardWrite();

			lock (InstanceMutationLock) {
				if (IsDisposed) {
					Logger.Warn("Attempted write manipulation on disposed resource of type: " + GetType().Name);
					return;
				}

				IntPtr outDataPtr;
				uint outRowStrideBytes, outSliceStrideBytes;
				InteropUtils.CallNative(
					NativeMethods.ResourceFactory_MapSubresource,
					RenderingModule.DeviceContext,
					ResourceHandle,
					GetSubresourceIndex(mipIndex),
					ResourceMapping.WriteDiscard,
					(IntPtr) (&outDataPtr),
					(IntPtr) (&outRowStrideBytes),
					(IntPtr) (&outSliceStrideBytes)
					).ThrowOnFailure();

				try {
					uint srcRowStrideBytes = MipWidth(mipIndex) * TexelSizeBytes;
					if (srcRowStrideBytes == outRowStrideBytes) { // If the data is aligned, just fast copy
						UnsafeUtils.CopyGenericArray(
							data,
							outDataPtr + (int)((writeOffsetX + writeOffsetY * Width) * TexelSizeBytes),
							TexelSizeBytes
						);
					}
					else { // Otherwise, copy row-by-row
						GCHandle pinnedDataHandle = GCHandle.Alloc(data.ContainingArray, GCHandleType.Pinned);
						try {
							IntPtr srcDataPtr = pinnedDataHandle.AddrOfPinnedObject();
							uint firstFullRowIndex = writeOffsetY + 1; // Inclusive
							uint lastFullRowIndex = firstFullRowIndex + ((data.Length - (Width - writeOffsetX)) / Width); // Exclusive

							// Write first row
							UnsafeUtils.MemCopy(srcDataPtr,
								outDataPtr + (int) (((firstFullRowIndex - 1) * outRowStrideBytes) + (writeOffsetX * TexelSizeBytes)), (Width - writeOffsetX) * TexelSizeBytes);

							int srcOffsetAtRowStart = (int) ((Width - writeOffsetX) * TexelSizeBytes);
							int dstOffsetAtRowStart = (int) (firstFullRowIndex * outRowStrideBytes);
							for (uint r = firstFullRowIndex; r < lastFullRowIndex; ++r) {
								UnsafeUtils.MemCopy(srcDataPtr + srcOffsetAtRowStart,
									outDataPtr + dstOffsetAtRowStart, srcRowStrideBytes);
								srcOffsetAtRowStart += (int) srcRowStrideBytes;
								dstOffsetAtRowStart += (int) outRowStrideBytes;
							}

							// Write last row
							UnsafeUtils.MemCopy(srcDataPtr + srcOffsetAtRowStart,
								outDataPtr + dstOffsetAtRowStart, ((data.Length - (Width - writeOffsetX)) % Width) * TexelSizeBytes);
						}
						finally {
							pinnedDataHandle.Free();	
						}
					}
				}
				finally {
					InteropUtils.CallNative(
						NativeMethods.ResourceFactory_UnmapSubresource,
						RenderingModule.DeviceContext,
						ResourceHandle,
						GetSubresourceIndex(mipIndex)
					).ThrowOnFailure();
				}
			}
		}

		/// <summary>
		/// Performs a <see cref="ResourceUsage.Write"/> on this texture, copying the supplied data to the resource.
		/// </summary>
		/// <param name="data">The data to write.
		/// <see cref="ArraySlice{T}.Length">Length</see> vertices will be copied from the given
		/// array slice. The copy will start from the specified <see cref="ArraySlice{T}.Offset">Offset</see> in the
		/// contained array.
		/// </param>
		/// <param name="dataDesc">The region of the selected <paramref name="mipIndex"/> to write to. The
		/// <see cref="SubresourceBox.Volume"/> (in this case, synonymous with 'area') of the box must be equal to the 
		/// <see cref="ArraySlice{T}.Length">Length</see> parameter of the supplied <paramref name="data"/>.</param>
		/// <param name="mipIndex">The mip level to write to. Only one mip level may be written to at a time. If this texture
		/// is not mipmapped, you must supply a value of <c>0U</c>.</param>
		/// <exception cref="ResourceOperationUnavailableException">Thrown if <see cref="BaseResource.CanDiscardWrite"/> is
		/// <c>false</c>.</exception>
		/// <exception cref="AssuranceFailedException">(Debug only) Thrown if the combination of supplied parameters would
		/// result in writing past the end of the texture in any dimension.</exception>
		public void Write(ArraySlice<TTexel> data, SubresourceBox dataDesc, uint mipIndex = 0U) {
			Assure.LessThan(
				mipIndex, NumMips,
				"Can not write to mip level " + mipIndex + ": Only " + NumMips + " present in texture."
			);
			Assure.Equal(
				data.Length,
				dataDesc.Volume,
				"Invalid parameters: Data length must equal the write target region area."
			);
			Assure.LessThanOrEqualTo(
				dataDesc.Left,
				Width,
				"Buffer overrun: Please ensure you are not attempting to write past the end of the texture."
			);
			Assure.LessThanOrEqualTo(
				dataDesc.Right,
				Width,
				"Buffer overrun: Please ensure you are not attempting to write past the end of the texture."
			);
			Assure.LessThanOrEqualTo(
				dataDesc.Top,
				Height,
				"Buffer overrun: Please ensure you are not attempting to write past the end of the texture."
			);
			Assure.LessThanOrEqualTo(
				dataDesc.Bottom,
				Height,
				"Buffer overrun: Please ensure you are not attempting to write past the end of the texture."
			);

			ThrowIfCannotWrite();

			lock (InstanceMutationLock) {
				if (IsDisposed) {
					Logger.Warn("Attempted write manipulation on disposed resource of type: " + GetType().Name);
					return;
				}

				GCHandle pinnedDataHandle = GCHandle.Alloc(data.ContainingArray, GCHandleType.Pinned);

				try {
					if (Usage.ShouldUpdateSubresourceRegion()) {
						InteropUtils.CallNative(
							NativeMethods.ResourceFactory_UpdateSubresourceRegion,
							RenderingModule.DeviceContext,
							ResourceHandle,
							GetSubresourceIndex(mipIndex),
							(IntPtr) (&dataDesc),
							pinnedDataHandle.AddrOfPinnedObject() + (int) (data.Offset * TexelSizeBytes),
							dataDesc.Width * TexelSizeBytes,
							(uint) Size
						).ThrowOnFailure();
					}
					else {
						Mutate_MapWrite(pinnedDataHandle, dataDesc, mipIndex);
					}
				}
				finally {
					pinnedDataHandle.Free();
				}
			}
		}

		private void Mutate_MapWrite(GCHandle pinnedDataHandle, SubresourceBox dataDesc, uint mipIndex) {
			LosgapSystem.InvokeOnMasterAsync(() => {
				IntPtr dstDataPtr;
				uint outRowStrideBytes, outSliceStrideBytes;
				InteropUtils.CallNative(
					NativeMethods.ResourceFactory_MapSubresource,
					RenderingModule.DeviceContext,
					ResourceHandle,
					GetSubresourceIndex(mipIndex),
					ResourceMapping.Write,
					(IntPtr) (&dstDataPtr),
					(IntPtr) (&outRowStrideBytes),
					(IntPtr) (&outSliceStrideBytes)
					).ThrowOnFailure();

				try {
					IntPtr srcDataPtr = pinnedDataHandle.AddrOfPinnedObject();
					for (uint srcRow = 0U, dstRowStart = outRowStrideBytes * dataDesc.Top;
						srcRow < dataDesc.Height;
						++srcRow, dstRowStart += outRowStrideBytes) {
						UnsafeUtils.MemCopy(srcDataPtr + (int) (dataDesc.Width * srcRow * TexelSizeBytes),
							dstDataPtr + (int) (dataDesc.Left + dstRowStart), dataDesc.Width * TexelSizeBytes);
					}
				}
				finally {
					InteropUtils.CallNative(
						NativeMethods.ResourceFactory_UnmapSubresource,
						RenderingModule.DeviceContext,
						ResourceHandle,
						GetSubresourceIndex(mipIndex)
					).ThrowOnFailure();
				}
			});
		}

		/// <summary>
		/// Performs a <see cref="ResourceUsage.StagingRead"/> on this texture, copying all the data from every mip level and
		/// concatenating it in to a single <typeparamref name="TTexel"/> array.
		/// </summary>
		/// <returns>An array of all texels in this resource, ordered first by ascending mip level, then by row.</returns>
		/// <exception cref="ResourceOperationUnavailableException">Thrown if <see cref="BaseResource.CanRead"/> is
		/// <c>false</c>.</exception>
		public override TTexel[] ReadAll() {
			ThrowIfCannotRead();
			return LosgapSystem.InvokeOnMaster(() => {
				TTexel[] result = new TTexel[SizeTexels];

				lock (InstanceMutationLock) {
					if (IsDisposed) {
						Logger.Warn("Attempted read manipulation on disposed resource of type: " + GetType().Name);
						return result;
					}
					GCHandle pinnedResult = GCHandle.Alloc(result, GCHandleType.Pinned);
					try {
						int dstOffsetBytes = 0;
						for (uint i = 0; i < NumMips; ++i) {
							IntPtr outDataPtr;
							uint outRowStrideBytes, outSliceStrideBytes;
							InteropUtils.CallNative(
								NativeMethods.ResourceFactory_MapSubresource,
								RenderingModule.DeviceContext,
								ResourceHandle,
								GetSubresourceIndex(i),
								ResourceMapping.Read,
								(IntPtr) (&outDataPtr),
								(IntPtr) (&outRowStrideBytes),
								(IntPtr) (&outSliceStrideBytes)
							).ThrowOnFailure();

							try {
								uint numRows = MipHeight(i);
								uint rowSizeBytes = MipWidth(i) * TexelSizeBytes;
								if (rowSizeBytes == outRowStrideBytes) { // If we can copy the whole lot in one go, do it
									UnsafeUtils.MemCopy(outDataPtr,
										pinnedResult.AddrOfPinnedObject() + dstOffsetBytes, rowSizeBytes * numRows);
									dstOffsetBytes += (int) (rowSizeBytes * numRows);
								}
								else { // Otherwise, copy row-by-row
									int srcOffsetBytes = 0;
									for (uint r = 0; r < numRows; ++r) {
										UnsafeUtils.MemCopy(outDataPtr + srcOffsetBytes,
											pinnedResult.AddrOfPinnedObject() + dstOffsetBytes, rowSizeBytes);
										dstOffsetBytes += (int) rowSizeBytes;
										srcOffsetBytes += (int) outRowStrideBytes;
									}
								}
							}
							finally {
								InteropUtils.CallNative(
									NativeMethods.ResourceFactory_UnmapSubresource,
									RenderingModule.DeviceContext,
									ResourceHandle,
									GetSubresourceIndex(i)
								).ThrowOnFailure();
							}
						}
					}
					finally {
						pinnedResult.Free();
					}
				}
			
				return result;
			});
		}

		/// <summary>
		/// Performs a <see cref="ResourceUsage.StagingRead"/> on this texture, 
		/// returning a view of the texel data at the given <paramref name="mipIndex"/>.
		/// </summary>
		/// <param name="mipIndex">The mip index to read data from. Must be less than <see cref="ITexture.NumMips"/>.</param>
		/// <returns>A <see cref="TexelArray2D{TTexel}"/> of the data.</returns>
		/// <exception cref="ResourceOperationUnavailableException">Thrown if <see cref="BaseResource.CanRead"/> is
		/// <c>false</c>.</exception>
		public TexelArray2D<TTexel> Read(uint mipIndex) {
			Assure.LessThan(
				mipIndex, NumMips,
				"Can not read from mip level " + mipIndex + ": Only " + NumMips + " present in texture."
			);

			ThrowIfCannotRead();

			TTexel[] data = LosgapSystem.InvokeOnMaster(() => {
				TTexel[] result = new TTexel[GetSizeTexels(mipIndex)];

				lock (InstanceMutationLock) {
					if (IsDisposed) {
						Logger.Warn("Attempted read manipulation on disposed resource of type: " + GetType().Name);
						return result;
					}
					GCHandle pinnedResult = GCHandle.Alloc(result, GCHandleType.Pinned);
					try {
						IntPtr outDataPtr;
						uint outRowStrideBytes, outSliceStrideBytes;
						InteropUtils.CallNative(
							NativeMethods.ResourceFactory_MapSubresource,
							RenderingModule.DeviceContext,
							ResourceHandle,
							GetSubresourceIndex(mipIndex),
							ResourceMapping.Read,
							(IntPtr) (&outDataPtr),
							(IntPtr) (&outRowStrideBytes),
							(IntPtr) (&outSliceStrideBytes)
						).ThrowOnFailure();

						try {
							uint numRows = MipHeight(mipIndex);
							uint rowSizeBytes = MipWidth(mipIndex) * TexelSizeBytes;
							if (rowSizeBytes == outRowStrideBytes) { // If we can copy the whole lot in one go, do it
								UnsafeUtils.MemCopy(outDataPtr,
									pinnedResult.AddrOfPinnedObject(), rowSizeBytes * numRows);
							}
							else { // Otherwise, copy row-by-row
								int dstOffsetBytes = 0;
								int srcOffsetBytes = 0;
								for (uint r = 0; r < numRows; ++r) {
									UnsafeUtils.MemCopy(outDataPtr + srcOffsetBytes,
										pinnedResult.AddrOfPinnedObject() + dstOffsetBytes, rowSizeBytes);
									dstOffsetBytes += (int) rowSizeBytes;
									srcOffsetBytes += (int) outRowStrideBytes;
								}
							}

							return result;
						}
						finally {
							InteropUtils.CallNative(
								NativeMethods.ResourceFactory_UnmapSubresource,
								RenderingModule.DeviceContext,
								ResourceHandle,
								GetSubresourceIndex(mipIndex)
							).ThrowOnFailure();
						}
					}
					finally {
						pinnedResult.Free();
					}
				}
			});

			return new TexelArray2D<TTexel>(data, MipWidth(mipIndex));
		}

		/// <summary>
		/// Performs a <see cref="ResourceUsage.StagingReadWrite"/> on this texture, allowing an in-place modification of the data
		/// through a read/write operation. This can be faster than an individual read and write operation in certain use cases.
		/// </summary>
		/// <param name="readWriteAction">An action that takes the supplied <see cref="RawResourceDataView2D{T}"/> and uses it to
		/// manipulate the data in-place. The supplied resource view is only valid for the duration of the invocation of this
		/// <see cref="Action"/>.</param>
		/// <param name="mipIndex">The mip index to read/write.</param>
		/// <exception cref="ResourceOperationUnavailableException">Thrown if <see cref="BaseResource.CanReadWrite"/> is
		/// <c>false</c>.</exception>
		public void ReadWrite(Action<RawResourceDataView2D<TTexel>> readWriteAction, uint mipIndex) {
			Assure.LessThan(
				mipIndex, NumMips,
				"Can not read from mip level " + mipIndex + ": Only " + NumMips + " present in texture."
			);

			ThrowIfCannotReadWrite();

			LosgapSystem.InvokeOnMaster(() => {
				lock (InstanceMutationLock) {
					if (IsDisposed) {
						Logger.Warn("Attempted read-write manipulation on disposed resource of type: " + GetType().Name);
						return;
					}
					IntPtr outDataPtr;
					uint outRowStrideBytes, outSliceStrideBytes;
					InteropUtils.CallNative(
						NativeMethods.ResourceFactory_MapSubresource,
						RenderingModule.DeviceContext,
						ResourceHandle,
						GetSubresourceIndex(mipIndex),
						ResourceMapping.ReadWrite,
						(IntPtr) (&outDataPtr),
						(IntPtr) (&outRowStrideBytes),
						(IntPtr) (&outSliceStrideBytes)
					).ThrowOnFailure();

					try {
						readWriteAction(new RawResourceDataView2D<TTexel>(
							outDataPtr,
							TexelSizeBytes,
							MipWidth(mipIndex),
							MipHeight(mipIndex),
							outRowStrideBytes
						));
					}
					finally {
						InteropUtils.CallNative(
							NativeMethods.ResourceFactory_UnmapSubresource,
							RenderingModule.DeviceContext,
							ResourceHandle,
							GetSubresourceIndex(mipIndex)
						).ThrowOnFailure();
					}
				}
			});
		}

		public ShaderTextureResourceView CreateView<TFormat>(uint firstMipIndex, uint numMips) where TFormat : ITexel {
			return DoCreateView(firstMipIndex, numMips, GetFormatForType(typeof(TFormat)));
		}

		/// <summary>
		/// Creates a new view to this texture.
		/// </summary>
		/// <param name="firstMipIndex">The index of the first mip-level in the texture that will be accessible through the returned view.</param>
		/// <param name="numMips">The number of mip-levels in this texture that will be accessible through the returned view.</param>
		/// <returns>A new resource view that permits reading data from this texture.</returns>
		public override ShaderTextureResourceView CreateView(uint firstMipIndex, uint numMips) {
			return DoCreateView(firstMipIndex, numMips, GetFormatForType(TexelFormat));
		}

		private ShaderTextureResourceView DoCreateView(uint firstMipIndex, uint numMips, ResourceFormat format) {
			if (firstMipIndex + numMips > NumMips || numMips == 0U) throw new ArgumentOutOfRangeException("numMips");
			if ((PermittedBindings & GPUBindings.ReadableShaderResource) != GPUBindings.ReadableShaderResource) {
				throw new InvalidOperationException("Can not create an shader resource view to a resource that was created without the "
					+ GPUBindings.ReadableShaderResource + " binding.");
			}

			ShaderResourceViewHandle outViewHandle;

			if (IsMultisampled) {
				Assure.Equal(firstMipIndex, 0U);
				Assure.Equal(numMips, 1U);
				if (IsArrayTexture) {
					InteropUtils.CallNative(
						NativeMethods.ResourceFactory_CreateSRVToTexture2DMSArray,
						RenderingModule.Device,
						(Texture2DResourceHandle) ResourceHandle,
						format,
						ArrayIndex,
						1U,
						(IntPtr) (&outViewHandle)
					).ThrowOnFailure();
				}
				else {
					InteropUtils.CallNative(
						NativeMethods.ResourceFactory_CreateSRVToTexture2DMS,
						RenderingModule.Device,
						(Texture2DResourceHandle) ResourceHandle,
						format,
						(IntPtr) (&outViewHandle)
					).ThrowOnFailure();
				}
			}
			else {
				if (IsArrayTexture) {
					InteropUtils.CallNative(
						NativeMethods.ResourceFactory_CreateSRVToTexture2DArray,
						RenderingModule.Device,
						(Texture2DResourceHandle) ResourceHandle,
						format,
						firstMipIndex,
						numMips,
						ArrayIndex,
						1U,
						(IntPtr) (&outViewHandle)
					).ThrowOnFailure();
				}
				else {
					InteropUtils.CallNative(
						NativeMethods.ResourceFactory_CreateSRVToTexture2D,
						RenderingModule.Device,
						(Texture2DResourceHandle) ResourceHandle,
						format,
						firstMipIndex,
						numMips,
						(IntPtr) (&outViewHandle)
					).ThrowOnFailure();
				}
			}

			return new ShaderTextureResourceView(outViewHandle, this, firstMipIndex, numMips);
		}

		/// <summary>
		/// Creates a new unordered access view to this texture.
		/// </summary>
		/// <param name="mipIndex">The mip-level to create a view to. Only one mip-level can be exposed in an <see cref="UnorderedAccessView"/>.</param>
		/// <returns>A new resource view that permits reading and writing data from/to this texture.</returns>
		public override UnorderedTextureAccessView CreateUnorderedAccessView(uint mipIndex) {
			if (mipIndex >= NumMips) throw new ArgumentOutOfRangeException("mipIndex");
			if (IsMultisampled) throw new InvalidOperationException("Can not create UAV to multisampled texture.");
			if ((PermittedBindings & GPUBindings.WritableShaderResource) != GPUBindings.WritableShaderResource) {
				throw new InvalidOperationException("Can not create an unordered access view to a resource that was created without the "
					+ GPUBindings.WritableShaderResource + " binding.");
			}

			UnorderedAccessViewHandle outViewHandle;

			if (IsArrayTexture) {
				InteropUtils.CallNative(
					NativeMethods.ResourceFactory_CreateUAVToTexture2DArray,
					RenderingModule.Device,
					(Texture2DResourceHandle) ResourceHandle,
					GetFormatForType(TexelFormat),
					mipIndex,
					ArrayIndex,
					1U,
					(IntPtr) (&outViewHandle)
				).ThrowOnFailure();
			}
			else {
				InteropUtils.CallNative(
					NativeMethods.ResourceFactory_CreateUAVToTexture2D,
					RenderingModule.Device,
					(Texture2DResourceHandle) ResourceHandle,
					GetFormatForType(TexelFormat),
					mipIndex,
					(IntPtr) (&outViewHandle)
				).ThrowOnFailure();
			}

			return new UnorderedTextureAccessView(outViewHandle, this, mipIndex);
		}


		/// <summary>
		/// Creates a new view to this resource that is capable of using the texture as a render target.
		/// </summary>
		/// <remarks>
		/// The texture's <see cref="ITexture.TexelFormat"/> must be <see cref="TexelFormat.RenderTarget"/>.
		/// This texture must not be mipmapped.
		/// The texture must have been created with the <see cref="GPUBindings.RenderTarget"/> GPU binding.
		/// </remarks>
		/// <param name="mipIndex">The mip level to create a render-target-view to.</param>
		/// <returns>A new render-target-view to the entirety of the texture.</returns>
		public RenderTargetView CreateRenderTargetView(uint mipIndex) {
			Assure.True(
				(PermittedBindings & GPUBindings.RenderTarget) == GPUBindings.RenderTarget,
				"Can not create a render target view to a texture that wasn't created with the " + GPUBindings.RenderTarget + " binding."
			);
			RenderTargetViewHandle outViewHandle;

			if (IsArrayTexture) {
				InteropUtils.CallNative(
					NativeMethods.ResourceFactory_CreateRTVToTexArr,
					RenderingModule.Device,
					(Texture2DResourceHandle) ResourceHandle,
					mipIndex,
					ArrayIndex,
					GetFormatForType(TexelFormat),
					(InteropBool) IsMultisampled,
					(IntPtr) (&outViewHandle)
				).ThrowOnFailure();
			}
			else {
				InteropUtils.CallNative(
					NativeMethods.ResourceFactory_CreateRTV,
					RenderingModule.Device,
					(Texture2DResourceHandle) ResourceHandle,
					mipIndex,
					GetFormatForType(TexelFormat),
					(InteropBool) IsMultisampled,
					(IntPtr) (&outViewHandle)
				).ThrowOnFailure();
			}

			return new RenderTargetView(outViewHandle, this, mipIndex);
		}

		/// <summary>
		/// Creates a new view to this resource that is capable of using the texture as a depth/stencil buffer.
		/// </summary>
		/// <remarks>
		/// The texture's <see cref="ITexture.TexelFormat"/> must be <see cref="TexelFormat.DepthStencil"/>.
		/// This texture must not be mipmapped.
		/// </remarks>
		/// <returns>A new depth-stencil-view to the entirety of the texture.</returns>
		public DepthStencilView CreateDepthStencilView(uint mipIndex) {
			return DoCreateDepthStencilView(mipIndex, GetFormatForType(TexelFormat));
		}

		public DepthStencilView CreateDepthStencilView<TFormat>(uint mipIndex) where TFormat : ITexel {
			return DoCreateDepthStencilView(mipIndex, GetFormatForType(typeof(TFormat)));
		}

		private DepthStencilView DoCreateDepthStencilView(uint mipIndex, ResourceFormat format) {
			Assure.True(
				(PermittedBindings & GPUBindings.DepthStencilTarget) == GPUBindings.DepthStencilTarget,
				"Can not create a depth stencil view to a texture that wasn't created with the " + GPUBindings.DepthStencilTarget + " binding."
			);
			DepthStencilViewHandle outViewHandle;

			if (IsArrayTexture) {
				InteropUtils.CallNative(
					NativeMethods.ResourceFactory_CreateDSVToTexArr,
					RenderingModule.Device,
					(Texture2DResourceHandle) ResourceHandle,
					mipIndex,
					ArrayIndex,
					format,
					(InteropBool) IsMultisampled,
					(IntPtr) (&outViewHandle)
				).ThrowOnFailure();
			}
			else {
				InteropUtils.CallNative(
					NativeMethods.ResourceFactory_CreateDSV,
					RenderingModule.Device,
					(Texture2DResourceHandle) ResourceHandle,
					mipIndex,
					format,
					(InteropBool) IsMultisampled,
					(IntPtr) (&outViewHandle)
				).ThrowOnFailure();
			}

			return new DepthStencilView(outViewHandle, this, mipIndex);
		}

		/// <summary>
		/// Used by the <see cref="BaseResource.ToString"/> implementation in <see cref="BaseResource"/> to create a child-class-specific
		/// string that provides additional data about this resource.
		/// </summary>
		/// <returns>A string in the format DATUM1 + <see cref="BaseResource.DESC_VALUE_SEPARATOR"/> + DATUM2 + 
		/// <see cref="BaseResource.DESC_VALUE_SEPARATOR"/> + DATUM3 etc</returns>
		protected override unsafe string CreateResourceDescString() {
			return
				"Tex2D<" + TexelFormat.Name + ">" + DESC_TYPE_SEPARATOR
				+ Width + "x" + Height + "tx" + DESC_VALUE_SEPARATOR
				+ (IsMultisampled ? "Multisampled" + DESC_VALUE_SEPARATOR : String.Empty)
				+ (IsMipmapped ? "Mipmap" + DESC_VALUE_SEPARATOR : String.Empty)
				+ (IsGlobalDetailTarget ? "DynDetail" + DESC_VALUE_SEPARATOR : String.Empty)
				+ (IsMipGenTarget ? "MipGenTarget" + DESC_VALUE_SEPARATOR : String.Empty)
				;
		}

		public byte[] ReadRaw() {
			byte[] dst = new byte[Size];
			TTexel[] src = ReadAll();

			var dstHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);

			try {
				UnsafeUtils.CopyGenericArray<TTexel>(src, dstHandle.AddrOfPinnedObject(), TexelSizeBytes);
			}
			finally {
				dstHandle.Free();
			}

			return dst;
		}
	}
}