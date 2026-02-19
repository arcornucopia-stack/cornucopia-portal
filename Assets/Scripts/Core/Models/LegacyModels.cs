using System;

namespace Cornucopia.Core.Models
{
    /// <summary>
    /// Consolidated legacy data models previously duplicated across multiple files.
    /// Used for Firebase Realtime Database serialization.
    /// </summary>

    /// <summary>
    /// Model metadata stored in cornucopia/models/{modelId}.
    /// Superset of all field variants (some files used 3-field, some 4-field version).
    /// </summary>
    [Serializable]
    public class LegacyModel
    {
        public string name;
        public string modelNamee;
        public string picPathh;
        public string question;

        public LegacyModel() { }

        public LegacyModel(string name, string modelName, string picPathh, string question = null)
        {
            this.name = name;
            this.modelNamee = modelName;
            this.picPathh = picPathh;
            this.question = question;
        }
    }

    /// <summary>
    /// Model feedback/analytics data stored in cornucopia/models/{modelId}/data.
    /// </summary>
    [Serializable]
    public class LegacyModelData
    {
        public int sent;
        public int saved;
        public int yes;
        public int no;
        public string rating;

        public LegacyModelData() { }

        public LegacyModelData(int sent, int saved, int yes, int no, string rating)
        {
            this.sent = sent;
            this.saved = saved;
            this.yes = yes;
            this.no = no;
            this.rating = rating;
        }
    }

    /// <summary>
    /// User profile details stored in cornucopia/users/{userId}.
    /// </summary>
    [Serializable]
    public class LegacyUserDetails
    {
        public string name;
        public string email;
        public string id;

        public LegacyUserDetails() { }

        public LegacyUserDetails(string name, string email, string id)
        {
            this.name = name;
            this.email = email;
            this.id = id;
        }
    }

    /// <summary>
    /// User activity stats stored in cornucopia/users/{userId}/userData.
    /// </summary>
    [Serializable]
    public class LegacyUserData
    {
        public int sent;
        public int saved;
        public int rated;

        public LegacyUserData() { }

        public LegacyUserData(int sent, int saved, int rated)
        {
            this.sent = sent;
            this.saved = saved;
            this.rated = rated;
        }
    }

    /// <summary>
    /// User's saved model reference stored in cornucopia/users/{userId}/models/{modelName}.
    /// Basic version with 3 fields (used by Home.cs, notification.cs).
    /// </summary>
    [Serializable]
    public class LegacyModelRef
    {
        public string MName;
        public bool saved;
        public string Rating;

        public LegacyModelRef() { }

        public LegacyModelRef(string MName, bool saved, string rating)
        {
            this.MName = MName;
            this.saved = saved;
            this.Rating = rating;
        }
    }

    /// <summary>
    /// Extended user model reference with answer field.
    /// Used by notifyModelUser.cs and allUserModel.cs for feedback tracking.
    /// </summary>
    [Serializable]
    public class LegacyModelRating
    {
        public string Rating;
        public string MName;
        public bool saved;
        public string answer;

        public LegacyModelRating() { }

        public LegacyModelRating(string rating, string MName, bool saved, string answer)
        {
            this.MName = MName;
            this.Rating = rating;
            this.saved = saved;
            this.answer = answer;
        }
    }
}
