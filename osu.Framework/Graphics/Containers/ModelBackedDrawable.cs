﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Manages dynamically displaying a custom <see cref="Drawable"/> based on a model object.
    /// Useful for replacing <see cref="Drawable"/>s on the fly.
    /// </summary>
    public abstract class ModelBackedDrawable<T> : CompositeDrawable where T : class
    {
        /// <summary>
        /// The currently displayed <see cref="Drawable"/>. Null if no drawable is displayed.
        /// </summary>
        protected Drawable DisplayedDrawable { get; private set; }

        /// <summary>
        /// The <see cref="IEqualityComparer{T}"/> used to compare models to ensure that <see cref="Drawable"/>s are not updated unnecessarily.
        /// </summary>
        protected readonly IEqualityComparer<T> Comparer;

        private T model;

        /// <summary>
        /// Gets or sets the model, potentially triggering the current <see cref="Drawable"/> to update.
        /// Subclasses should expose this via a nicer property name to better represent the data being set.
        /// </summary>
        protected T Model
        {
            get => model;
            set
            {
                if (model == null && value == null)
                    return;

                if (Comparer.Equals(model, value))
                    return;

                model = value;

                if (IsLoaded)
                    updateDrawable();
            }
        }

        /// <summary>
        /// Constructs a new <see cref="ModelBackedDrawable{T}"/> with the default <typeparamref name="T"/> equality comparer.
        /// </summary>
        protected ModelBackedDrawable()
            : this(EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ModelBackedDrawable{T}"/> with a custom equality function.
        /// </summary>
        /// <param name="func">The equality function.</param>
        protected ModelBackedDrawable(Func<T, T, bool> func)
            : this(new FuncEqualityComparer<T>(func))
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ModelBackedDrawable{T}"/> with a custom <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">The comparer to use.</param>
        protected ModelBackedDrawable(IEqualityComparer<T> comparer)
        {
            Comparer = comparer;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateDrawable();
        }

        private DelayedLoadWrapper currentWrapper;
        private bool placeholderDisplayed;

        private void updateDrawable()
        {
            if (model == null)
                loadPlaceholder();
            else
            {
                if (FadeOutImmediately) loadPlaceholder();
                loadDrawable(CreateDrawable(model), false);
            }
        }

        private void loadPlaceholder()
        {
            if (placeholderDisplayed)
                return;

            var placeholder = CreateDrawable(null);

            loadDrawable(placeholder, true);

            // in the case a placeholder has not been specified, this should not be set as to allow for a potential runtime change
            // of placeholder logic on a future load operation.
            placeholderDisplayed = placeholder != null;
        }

        private void loadDrawable(Drawable newDrawable, bool isPlaceholder)
        {
            // Remove the previous wrapper if the inner drawable hasn't finished loading.
            // We check IsLoaded on the content rather than DelayedLoadCompleted so that we can ensure that finishLoad() has not been called and DisplayedDrawable hasn't been updated
            if (currentWrapper?.Content.IsLoaded == false)
            {
                // Using .Expire() will be one frame too late, since children's lifetime has already been updated this frame
                RemoveInternal(currentWrapper);
                DisposeChildAsync(currentWrapper);
            }

            currentWrapper = null;

            if (newDrawable != null)
            {
                AddInternal(isPlaceholder ? newDrawable : currentWrapper = CreateDelayedLoadWrapper(newDrawable, LoadDelay));

                if (isPlaceholder)
                {
                    // Although the drawable is technically not loaded, it does have a clock and we need DisplayedDrawable to be updated instantly
                    finishLoad();
                }
                else
                    newDrawable.OnLoadComplete += _ => finishLoad();
            }
            else
                finishLoad();

            void finishLoad()
            {
                var currentDrawable = DisplayedDrawable;

                var transform = ReplaceDrawable(currentDrawable, newDrawable) ?? (currentDrawable ?? newDrawable)?.DelayUntilTransformsFinished();
                transform?.OnComplete(_ => currentDrawable?.Expire());

                DisplayedDrawable = newDrawable;
                placeholderDisplayed = isPlaceholder;
            }
        }

        /// <summary>
        /// Determines whether the current <see cref="Drawable"/> should fade out straight away when switching to a new model,
        /// or whether it should wait until the new <see cref="Drawable"/> has finished loading.
        /// </summary>
        protected virtual bool FadeOutImmediately => false;

        /// <summary>
        /// The time in milliseconds that <see cref="Drawable"/>s will fade in and out.
        /// </summary>
        protected virtual double FadeDuration => 1000;

        /// <summary>
        /// The delay in milliseconds before <see cref="Drawable"/>s will begin loading.
        /// </summary>
        protected virtual double LoadDelay => 0;

        /// <summary>
        /// Allows subclasses to customise the <see cref="DelayedLoadWrapper"/>.
        /// </summary>
        protected virtual DelayedLoadWrapper CreateDelayedLoadWrapper(Drawable content, double timeBeforeLoad) =>
            new DelayedLoadWrapper(content, timeBeforeLoad);

        /// <summary>
        /// Override to instantiate a custom <see cref="Drawable"/> based on the passed model.
        /// May be null to indicate that the model has no visual representation,
        /// in which case the placeholder will be used if it exists.
        /// </summary>
        /// <param name="model">The model that the <see cref="Drawable"/> should represent.</param>
        protected abstract Drawable CreateDrawable(T model);

        /// <summary>
        /// Returns a <see cref="TransformSequence{Drawable}"/> that replaces the given <see cref="Drawable"/>s.
        /// Default functionality is to fade in the target from zero, or if it is null, to fade out the source.
        /// </summary>
        /// <returns>The drawable.</returns>
        /// <param name="source">The <see cref="Drawable"/> to be replaced.</param>
        /// <param name="target">The <see cref="Drawable"/> we are replacing with.</param>
        protected virtual TransformSequence<Drawable> ReplaceDrawable(Drawable source, Drawable target) =>
            target?.FadeInFromZero(FadeDuration, Easing.OutQuint) ?? source?.FadeOut(FadeDuration, Easing.OutQuint);
    }
}
